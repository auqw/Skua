using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Skua.App.Avalonia.Flash;

public sealed class WebSocketFlashHostTransport : IFlashHostTransport
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ElectronFlashHostOptions _options;
    private readonly string _token;
    private readonly ConcurrentDictionary<long, TaskCompletionSource<FlashHostResponse>> _pending = new();
    private static readonly HttpClient HttpClient = new();

    private readonly CancellationTokenSource _cts = new();
    private HttpListener? _listener;
    private string? _servedSwfPath;
    private WebSocket? _socket;
    private Process? _process;
    private Task? _receiveLoop;
    private readonly SemaphoreSlim _callLock = new(1, 1);
    private TaskCompletionSource _connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _port;

    public WebSocketFlashHostTransport(ElectronFlashHostOptions options)
    {
        _options = options;
        _token = Guid.NewGuid().ToString("N");
    }

    public event EventHandler<FlashHostCallbackEventArgs>? CallbackReceived;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        LinuxFlashTrace.Event("transport", "start-begin");
        if (_socket is not null && _socket.State == WebSocketState.Open)
        {
            LinuxFlashTrace.Event("transport", "start-skip", ("socket", _socket.State));
            return;
        }

        _options.Validate();
        LinuxFlashTrace.Event("transport", "options-valid", ("electron", _options.ElectronPath), ("host", _options.HostDirectory), ("swf", _options.SwfPath), ("plugin", _options.FlashPluginPath));
        EnsureFlashTrustFile(_options.SwfPath, _options.HostDirectory);

        _port = GetFreeLoopbackPort();
        _servedSwfPath = CreatePortPatchedSwf(_options.SwfPath, _port);
        _listener = new HttpListener();
        string prefix = $"http://127.0.0.1:{_port}/";
        string gamePrefix = $"http://*:{_port}/";
        _listener.Prefixes.Add(gamePrefix);
        _listener.Start();
        LinuxFlashTrace.Event("transport", "http-listener-started", ("prefix", prefix), ("gamePrefix", gamePrefix));

        _ = Task.Run(() => AcceptLoopAsync(_cts.Token), _cts.Token);
        StartElectronHost(prefix);
        LinuxFlashTrace.Event("transport", "electron-started-waiting-ws");

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        await _connected.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
        LinuxFlashTrace.Event("transport", "websocket-connected");
    }

    public async Task<FlashHostResponse> SendAsync(FlashHostRequest request, CancellationToken cancellationToken = default)
    {
        WebSocket socket = _socket ?? throw new InvalidOperationException("Flash host WebSocket is not connected.");
        if (socket.State != WebSocketState.Open)
            throw new InvalidOperationException($"Flash host WebSocket is not open: {socket.State}");

        Stopwatch stopwatch = Stopwatch.StartNew();
        LinuxFlashTrace.Event("transport", "rpc-wait-lock", ("id", request.Id), ("fn", request.Function), ("pending", _pending.Count));
        await _callLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        TaskCompletionSource<FlashHostResponse> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            _pending[request.Id] = pending;
            string json = JsonSerializer.Serialize(request, JsonOptions);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            LinuxFlashTrace.Event("transport", "rpc-send", ("id", request.Id), ("fn", request.Function), ("bytes", bytes.Length), ("pending", _pending.Count), ("xml", LinuxFlashTrace.XmlSummary(request.Xml)));
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
            LinuxFlashTrace.Event("transport", "rpc-sent", ("id", request.Id), ("fn", request.Function), ("elapsedMs", stopwatch.ElapsedMilliseconds));

            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(30));
            try
            {
                FlashHostResponse response = await pending.Task.WaitAsync(timeout.Token).ConfigureAwait(false);
                LinuxFlashTrace.Event("transport", "rpc-complete", ("id", request.Id), ("fn", request.Function), ("ok", response.Ok), ("elapsedMs", stopwatch.ElapsedMilliseconds), ("pending", _pending.Count));
                return response;
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && !_cts.IsCancellationRequested)
            {
                LinuxFlashTrace.Event("transport", "rpc-timeout", ("id", request.Id), ("fn", request.Function), ("elapsedMs", stopwatch.ElapsedMilliseconds), ("pending", _pending.Count), ("socket", socket.State));
                throw new TimeoutException($"Flash host call timed out: id={request.Id} function={request.Function}", ex);
            }
        }
        catch (Exception ex) when (ex is not TimeoutException)
        {
            LinuxFlashTrace.Event("transport", "rpc-error", ("id", request.Id), ("fn", request.Function), ("elapsedMs", stopwatch.ElapsedMilliseconds), ("type", ex.GetType().Name), ("message", ex.Message));
            throw;
        }
        finally
        {
            _pending.TryRemove(request.Id, out _);
            _callLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        LinuxFlashTrace.Event("transport", "dispose-begin", ("pending", _pending.Count));
        _cts.Cancel();
        FailPending(new OperationCanceledException("Flash host transport disposed."));

        try
        {
            if (_socket is { State: WebSocketState.Open })
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Skua closing", CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LinuxFlashTrace.Event("transport", "dispose-close-error", ("type", ex.GetType().Name), ("message", ex.Message));
        }

        try { _listener?.Stop(); } catch { }
        try { _listener?.Close(); } catch { }

        try
        {
            if (_process is { HasExited: false })
            {
                LinuxFlashTrace.Event("transport", "electron-kill", ("pid", _process.Id));
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync().ConfigureAwait(false);
            }
        }
        catch { }

        _socket?.Dispose();
        _listener = null;
        _process?.Dispose();
        _process = null;
        _cts.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && _listener is { IsListening: true } listener)
            {
                HttpListenerContext context = await listener.GetContextAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
                if (context.Request.Url?.AbsolutePath == "/skua" && context.Request.IsWebSocketRequest)
                {
                    if (context.Request.QueryString["token"] != _token)
                    {
                        LinuxFlashTrace.Event("transport", "websocket-reject", ("remote", context.Request.RemoteEndPoint), ("path", context.Request.Url?.PathAndQuery));
                        context.Response.StatusCode = 403;
                        context.Response.Close();
                        continue;
                    }

                    HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
                    _socket = wsContext.WebSocket;
                    LinuxFlashTrace.Event("transport", "websocket-accepted", ("remote", context.Request.RemoteEndPoint));
                    _connected.TrySetResult();
                    _receiveLoop = Task.Run(() => ReceiveLoopAsync(_socket, cancellationToken), cancellationToken);
                    continue;
                }

                LinuxFlashTrace.Event("http", "request", ("method", context.Request.HttpMethod), ("path", context.Request.Url?.PathAndQuery), ("contentLength", context.Request.ContentLength64));

                if (context.Request.Url?.AbsolutePath == "/crossdomain.xml" || context.Request.Url?.AbsolutePath == "/g/crossdomain.xml" || context.Request.Url?.AbsolutePath == "/game/crossdomain.xml")
                {
                    await ServeBytesAsync(context, Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><cross-domain-policy><allow-access-from domain=\"*\" to-ports=\"*\" /></cross-domain-policy>"), "text/x-cross-domain-policy", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (context.Request.Url?.AbsolutePath == "/skua.html")
                {
                    await ServeFileAsync(context, Path.Combine(_options.HostDirectory, "skua.html"), "text/html", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (context.Request.Url?.AbsolutePath == "/skua.swf")
                {
                    await ServeFileAsync(context, _servedSwfPath ?? _options.SwfPath, "application/x-shockwave-flash", cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (ShouldProxyToAqw(context.Request.Url?.AbsolutePath))
                {
                    await ProxyAqwAsync(context, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                LinuxFlashTrace.Event("http", "not-found", ("path", context.Request.Url?.PathAndQuery));
                context.Response.StatusCode = 404;
                context.Response.Close();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LinuxFlashTrace.Event("transport", "accept-loop-error", ("type", ex.GetType().Name), ("message", ex.Message));
            _connected.TrySetException(ex);
            FailPending(ex);
        }
    }

    private async Task ReceiveLoopAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[64 * 1024];
        LinuxFlashTrace.Event("transport", "receive-loop-begin");
        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                using MemoryStream ms = new();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        LinuxFlashTrace.Event("transport", "websocket-close-frame", ("status", result.CloseStatus), ("description", result.CloseStatusDescription));
                        return;
                    }
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                string json = Encoding.UTF8.GetString(ms.ToArray());
                LinuxFlashTrace.Event("transport", "ws-recv", ("bytes", ms.Length), ("preview", LinuxFlashTrace.Preview(json)));
                DispatchMessage(json);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LinuxFlashTrace.Event("transport", "receive-loop-error", ("type", ex.GetType().Name), ("message", ex.Message));
            FailPending(ex);
        }
        finally
        {
            LinuxFlashTrace.Event("transport", "receive-loop-end", ("socket", socket.State), ("pending", _pending.Count));
            FailPending(new IOException($"Flash host WebSocket receive loop ended with socket state {socket.State}."));
        }
    }

    private void DispatchMessage(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            string? type = root.TryGetProperty("type", out JsonElement typeEl) ? typeEl.GetString() : null;

            if (type == "flashCall")
            {
                string function = root.GetProperty("function").GetString() ?? string.Empty;
                object?[] args = root.TryGetProperty("args", out JsonElement argsEl) && argsEl.ValueKind == JsonValueKind.Array
                    ? argsEl.EnumerateArray().Select(ConvertJsonElement).ToArray()
                    : [];
                LinuxFlashTrace.Event("transport", "callback-received", ("fn", function), ("args", LinuxFlashTrace.ArgSummary(args)));
                CallbackReceived?.Invoke(this, new FlashHostCallbackEventArgs(function, args));
                LinuxFlashTrace.Event("transport", "callback-dispatched", ("fn", function));
                return;
            }

            if (root.TryGetProperty("id", out JsonElement idEl))
            {
                long id = idEl.GetInt64();
                FlashHostResponse response = JsonSerializer.Deserialize<FlashHostResponse>(json, JsonOptions) ?? new FlashHostResponse(id, false, Error: "Invalid response");
                if (_pending.TryGetValue(id, out TaskCompletionSource<FlashHostResponse>? pending))
                {
                    LinuxFlashTrace.Event("transport", "response-received", ("id", id), ("ok", response.Ok), ("error", response.Error), ("xml", LinuxFlashTrace.XmlSummary(response.Xml)));
                    pending.TrySetResult(response);
                }
                else
                {
                    LinuxFlashTrace.Event("transport", "response-unknown", ("id", id), ("ok", response.Ok), ("error", response.Error));
                }
                return;
            }

            LinuxFlashTrace.Event("transport", "message-unknown", ("type", type), ("preview", LinuxFlashTrace.Preview(json)));
        }
        catch (Exception ex)
        {
            LinuxFlashTrace.Event("transport", "dispatch-error", ("type", ex.GetType().Name), ("message", ex.Message), ("preview", LinuxFlashTrace.Preview(json)));
        }
    }

    private static string CreatePortPatchedSwf(string swfPath, int port)
    {
        byte[] swf = File.ReadAllBytes(swfPath);
        if (swf.Length < 8 || swf[0] != (byte)'C' || swf[1] != (byte)'W' || swf[2] != (byte)'S')
            return swfPath;

        byte[] body;
        using (MemoryStream compressed = new(swf, 8, swf.Length - 8))
        using (ZLibStream zlib = new(compressed, CompressionMode.Decompress))
        using (MemoryStream decompressed = new())
        {
            zlib.CopyTo(decompressed);
            body = decompressed.ToArray();
        }

        byte[] oldUrl = Encoding.ASCII.GetBytes("https://game.aq.com/game/");
        string replacement = $"http://game.aq.com/game/";
        int index = IndexOf(body, oldUrl);
        if (index < 0)
        {
            LinuxFlashTrace.Event("swf", "base-url-patch-skip", ("reason", "pattern-missing"));
            return swfPath;
        }
        if (replacement.Length != oldUrl.Length)
        {
            LinuxFlashTrace.Event("swf", "base-url-patch-skip", ("reason", "length-mismatch"), ("oldLength", oldUrl.Length), ("newLength", replacement.Length), ("replacement", replacement));
            return swfPath;
        }
        Encoding.ASCII.GetBytes(replacement).CopyTo(body, index);

        LinuxFlashTrace.Event("swf", "base-url-patched", ("replacement", replacement));
        string tempPath = Path.Combine(Path.GetTempPath(), $"skua-linux-{port}.swf");
        using FileStream output = File.Create(tempPath);
        output.Write(swf, 0, 8);
        using (ZLibStream zlib = new(output, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(body);
        return tempPath;
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return i;
        }
        return -1;
    }

    private static bool ShouldProxyToAqw(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;
        if (path is "/skua" or "/skua.html" or "/skua.swf" or "/crossdomain.xml")
            return false;
        return true;
    }

    private static async Task ProxyAqwAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        string pathAndQuery = context.Request.Url?.PathAndQuery ?? "/";
        string upstream = pathAndQuery.StartsWith("/game/", StringComparison.Ordinal)
            ? "https://game.aq.com" + pathAndQuery
            : "https://game.aq.com/game" + pathAndQuery;
        using HttpRequestMessage request = new(new HttpMethod(context.Request.HttpMethod), upstream);
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.163 Safari/537.36");
        request.Headers.Accept.ParseAdd(context.Request.Headers["Accept"] ?? "*/*");
        request.Headers.Referrer = new Uri("https://game.aq.com/game/gamefiles/Loader3.swf?ver=a");

        if (context.Request.HasEntityBody)
        {
            using MemoryStream body = new();
            await context.Request.InputStream.CopyToAsync(body, cancellationToken).ConfigureAwait(false);
            request.Content = new ByteArrayContent(body.ToArray());
            if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
                request.Content.Headers.TryAddWithoutValidation("Content-Type", context.Request.ContentType);
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (AqwGameSwfPatcher.IsAqwGameSwf(context.Request.Url?.AbsolutePath))
            bytes = AqwGameSwfPatcher.PatchSharedObjectSecureFlag(bytes, message => LinuxFlashTrace.Event("proxy", "swf-patch", ("message", message)));
        LinuxFlashTrace.Event("proxy", "response", ("method", context.Request.HttpMethod), ("path", context.Request.Url?.PathAndQuery), ("status", (int)response.StatusCode), ("upstream", upstream), ("bytes", bytes.Length), ("elapsedMs", stopwatch.ElapsedMilliseconds));
        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        context.Response.Close();
    }

    private static async Task ServeBytesAsync(HttpListenerContext context, byte[] bytes, string contentType, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = contentType;
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        context.Response.Close();
    }

    private static async Task ServeFileAsync(HttpListenerContext context, string path, string contentType, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
            return;
        }

        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        context.Response.StatusCode = 200;
        context.Response.ContentType = contentType;
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        context.Response.Close();
    }

    private void StartElectronHost(string httpPrefix)
    {
        const string gameOrigin = "http://game.aq.com/";
        string wsUrl = $"ws://game.aq.com/skua?token={Uri.EscapeDataString(_token)}";
        string hostUrl = gameOrigin + "skua.html";
        string swfUrl = gameOrigin + "skua.swf";
        string args = $"\"{_options.HostDirectory}\" -- --ws=\"{wsUrl}\" --swf=\"{swfUrl}\" --host-url=\"{hostUrl}\" --host-resolver-rules=\"MAP game.aq.com 127.0.0.1:{_port}\" --flash-plugin=\"{_options.FlashPluginPath}\"";
        LinuxFlashTrace.Event("electron", "start", ("path", _options.ElectronPath), ("args", args));
        ProcessStartInfo startInfo = new()
        {
            FileName = _options.ElectronPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = _options.HostDirectory
        };
        startInfo.Environment.Remove("NODE_OPTIONS");
        _process = Process.Start(startInfo);

        if (_process is null)
            throw new InvalidOperationException("Failed to start Electron Flash host.");

        LinuxFlashTrace.Event("electron", "started", ("pid", _process.Id));
        _process.EnableRaisingEvents = true;
        _process.Exited += (_, _) =>
        {
            LinuxFlashTrace.Event("electron", "exited", ("pid", _process.Id), ("exitCode", _process.ExitCode), ("pending", _pending.Count));
            FailPending(new IOException($"Electron Flash host exited with code {_process.ExitCode}."));
        };
        _process.OutputDataReceived += (_, e) => { if (e.Data is not null) LinuxFlashTrace.Event("electron", "stdout", ("text", e.Data)); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data is not null) LinuxFlashTrace.Event("electron", "stderr", ("text", e.Data)); };
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    private static int GetFreeLoopbackPort()
    {
        using System.Net.Sockets.TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static void EnsureFlashTrustFile(params string[] paths)
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string trustDir = Path.Combine(home, ".macromedia", "Flash_Player", "#Security", "FlashPlayerTrust");
        Directory.CreateDirectory(trustDir);
        string trustFile = Path.Combine(trustDir, "Skua.cfg");
        File.WriteAllLines(trustFile, paths.Select(Path.GetFullPath).Distinct());
        LinuxFlashTrace.Event("flash-trust", "updated", ("path", trustFile), ("entries", string.Join(",", paths.Select(Path.GetFullPath).Distinct())));
    }

    private void FailPending(Exception exception)
    {
        foreach ((long id, TaskCompletionSource<FlashHostResponse> pending) in _pending.ToArray())
        {
            if (_pending.TryRemove(id, out _))
            {
                LinuxFlashTrace.Event("transport", "pending-failed", ("id", id), ("type", exception.GetType().Name), ("message", exception.Message));
                pending.TrySetException(exception);
            }
        }
    }

    private static object? ConvertJsonElement(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt64(out long l) ? l : el.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => el.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            JsonValueKind.Object => el.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => null
        };
    }
}
