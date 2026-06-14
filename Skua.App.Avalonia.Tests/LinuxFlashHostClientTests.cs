using Skua.App.Avalonia.Flash;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace Skua.App.Avalonia.Tests;

public sealed class LinuxFlashHostClientTests
{
    [Fact]
    public void Call_SendsExternalInterfaceXmlAndDecodesReturn()
    {
        var transport = new FakeTransport(new FlashHostResponse(1, true, "<number>42</number>"));
        using var client = new LinuxFlashHostClient(transport);

        int value = client.Call<int>("answer", "x");

        Assert.Equal(42, value);
        Assert.True(transport.Started);
        Assert.Single(transport.Requests);
        Assert.Equal("call", transport.Requests[0].Type);
        Assert.Equal("answer", transport.Requests[0].Function);
        Assert.Equal("<invoke name=\"answer\" returntype=\"xml\"><arguments><string>x</string></arguments></invoke>", transport.Requests[0].Xml);
    }

    [Fact]
    public void HandleCallbackXml_RaisesFlashCall()
    {
        var transport = new FakeTransport(new FlashHostResponse(1, true, "<null />"));
        using var client = new LinuxFlashHostClient(transport);
        string? function = null;
        object[]? args = null;
        client.FlashCall += (name, values) => { function = name; args = values; };

        client.HandleCallbackXml("<invoke name=\"packet\"><arguments><string>%xt%</string><number>2</number></arguments></invoke>");

        Assert.Equal("packet", function);
        Assert.Equal(["%xt%", 2d], args);
    }

    [Fact]
    public void TransportCallback_IsQueuedAndDoesNotBlockTransportEventThread()
    {
        var transport = new FakeTransport(new FlashHostResponse(1, true, "<null />"));
        using var client = new LinuxFlashHostClient(transport);
        using ManualResetEventSlim callbackEntered = new(false);
        using ManualResetEventSlim releaseCallback = new(false);
        client.FlashCall += (_, _) =>
        {
            callbackEntered.Set();
            releaseCallback.Wait(TimeSpan.FromSeconds(5));
        };

        Stopwatch stopwatch = Stopwatch.StartNew();
        transport.RaiseCallback("pext", "payload");
        stopwatch.Stop();

        Assert.True(stopwatch.ElapsedMilliseconds < 200, $"Transport callback blocked for {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(callbackEntered.Wait(TimeSpan.FromSeconds(1)));
        releaseCallback.Set();
    }

    private sealed class FakeTransport : IFlashHostTransport
    {
        private readonly ConcurrentQueue<FlashHostResponse> _responses = new();
        public event EventHandler<FlashHostCallbackEventArgs>? CallbackReceived;
        public List<FlashHostRequest> Requests { get; } = [];
        public bool Started { get; private set; }

        public FakeTransport(params FlashHostResponse[] responses)
        {
            foreach (var response in responses)
                _responses.Enqueue(response);
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            Started = true;
            return Task.CompletedTask;
        }

        public Task<FlashHostResponse> SendAsync(FlashHostRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.FromResult(_responses.TryDequeue(out var response) ? response with { Id = request.Id } : new FlashHostResponse(request.Id, true, "<null />"));
        }

        public void RaiseCallback(string function, params object?[] args)
        {
            CallbackReceived?.Invoke(this, new FlashHostCallbackEventArgs(function, args));
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
