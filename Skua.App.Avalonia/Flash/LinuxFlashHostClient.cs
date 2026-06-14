using Skua.Core.Interfaces;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Skua.App.Avalonia.Flash;

public sealed class LinuxFlashHostClient : IDisposable
{
    private readonly IFlashHostTransport _transport;
    private readonly CancellationTokenSource _callbackCts = new();
    private readonly Channel<FlashHostCallbackEventArgs> _callbackQueue = Channel.CreateUnbounded<FlashHostCallbackEventArgs>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false
    });
    private readonly Task _callbackPump;
    private int _nextId;
    private bool _started;

    public LinuxFlashHostClient(IFlashHostTransport transport)
    {
        _transport = transport;
        _transport.CallbackReceived += Transport_CallbackReceived;
        _callbackPump = Task.Run(() => CallbackPumpAsync(_callbackCts.Token));
    }

    public event FlashCallHandler? FlashCall;

    public void Start()
    {
        LinuxFlashTrace.Event("client", "start-request");
        EnsureStarted();
        LinuxFlashTrace.Event("client", "start-done");
    }

    public T? Call<T>(string function, params object?[] args)
    {
        object? value = Call(function, typeof(T), args);
        if (value is null)
            return default;
        if (value is T typed)
            return typed;
        return (T)Convert.ChangeType(value, typeof(T));
    }

    public object? Call(string function, Type returnType, params object?[] args)
    {
        EnsureStarted();
        int id = Interlocked.Increment(ref _nextId);
        string xml = ExternalInterfaceXmlCodec.EncodeInvoke(function, args);
        FlashHostRequest request = new(id, "call", function, xml);
        Stopwatch stopwatch = Stopwatch.StartNew();
        LinuxFlashTrace.Event("client", "call-begin", ("id", id), ("fn", function), ("returnType", returnType.Name), ("args", LinuxFlashTrace.ArgSummary(args)), ("xml", LinuxFlashTrace.XmlSummary(xml)));
        FlashHostResponse response;
        try
        {
            response = _transport.SendAsync(request).GetAwaiter().GetResult();
            LinuxFlashTrace.Event("client", "call-response", ("id", id), ("fn", function), ("ok", response.Ok), ("elapsedMs", stopwatch.ElapsedMilliseconds), ("xml", LinuxFlashTrace.XmlSummary(response.Xml)), ("error", response.Error));
        }
        catch (Exception ex)
        {
            LinuxFlashTrace.Event("client", "call-exception", ("id", id), ("fn", function), ("elapsedMs", stopwatch.ElapsedMilliseconds), ("type", ex.GetType().Name), ("message", ex.Message));
            throw;
        }

        if (!response.Ok)
            throw new InvalidOperationException(response.Error ?? $"Flash host call failed: {function}");

        object? decoded = ExternalInterfaceXmlCodec.DecodeReturn(response.Xml ?? "<null />");
        if (decoded is null)
            return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
        if (returnType == typeof(string))
            return decoded.ToString();
        if (returnType.IsInstanceOfType(decoded))
            return decoded;
        return Convert.ChangeType(decoded, returnType);
    }

    public string? Call(string function, params object?[] args)
    {
        return Call(function, typeof(string), args)?.ToString();
    }

    public void HandleCallbackXml(string xml)
    {
        FlashCallback callback = ExternalInterfaceXmlCodec.DecodeCallback(xml);
        FlashCall?.Invoke(callback.Function, callback.Args!);
    }

    public void Dispose()
    {
        _transport.CallbackReceived -= Transport_CallbackReceived;
        _callbackQueue.Writer.TryComplete();
        _callbackCts.Cancel();
        try
        {
            _callbackPump.Wait(TimeSpan.FromSeconds(2));
        }
        catch { }
        _callbackCts.Dispose();
        _transport.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private void Transport_CallbackReceived(object? sender, FlashHostCallbackEventArgs e)
    {
        bool queued = _callbackQueue.Writer.TryWrite(e);
        LinuxFlashTrace.Event("client", queued ? "callback-queued" : "callback-dropped", ("fn", e.Function), ("args", LinuxFlashTrace.ArgSummary(e.Args)));
    }

    private async Task CallbackPumpAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (FlashHostCallbackEventArgs callback in _callbackQueue.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                LinuxFlashTrace.Event("client", "callback-begin", ("fn", callback.Function), ("args", LinuxFlashTrace.ArgSummary(callback.Args)));
                try
                {
                    FlashCall?.Invoke(callback.Function, callback.Args!);
                    LinuxFlashTrace.Event("client", "callback-end", ("fn", callback.Function), ("elapsedMs", stopwatch.ElapsedMilliseconds));
                }
                catch (Exception ex)
                {
                    LinuxFlashTrace.Event("client", "callback-error", ("fn", callback.Function), ("elapsedMs", stopwatch.ElapsedMilliseconds), ("type", ex.GetType().Name), ("message", ex.Message));
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            LinuxFlashTrace.Event("client", "callback-pump-error", ("type", ex.GetType().Name), ("message", ex.Message));
        }
    }

    private void EnsureStarted()
    {
        if (_started)
            return;
        _transport.StartAsync().GetAwaiter().GetResult();
        _started = true;
    }
}
