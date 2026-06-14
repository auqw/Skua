using System;

namespace Skua.App.Avalonia.Flash;

public sealed class ElectronFlashHostClient : IFlashHostClient
{
    private readonly LinuxFlashHostClient _client;

    public ElectronFlashHostClient()
        : this(ElectronFlashHostOptions.FromEnvironment())
    {
    }

    public ElectronFlashHostClient(ElectronFlashHostOptions options)
        : this(new WebSocketFlashHostTransport(options))
    {
    }

    public ElectronFlashHostClient(IFlashHostTransport transport)
    {
        _client = new LinuxFlashHostClient(transport);
        _client.FlashCall += (function, args) => FlashCalled?.Invoke(this, new FlashHostCallEventArgs(function, args!));
    }

    public event EventHandler<FlashHostCallEventArgs>? FlashCalled;

    public void Start()
    {
        _client.Start();
    }

    public string? Call(string function, params object?[] args)
    {
        return _client.Call(function, args);
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
