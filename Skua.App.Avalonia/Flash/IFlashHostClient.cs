using System;

namespace Skua.App.Avalonia.Flash;

public interface IFlashHostClient : IDisposable
{
    event EventHandler<FlashHostCallEventArgs>? FlashCalled;

    void Start();

    string? Call(string function, params object?[] args);
}

public sealed class FlashHostCallEventArgs : EventArgs
{
    public FlashHostCallEventArgs(string function, object?[] args)
    {
        Function = function;
        Args = args;
    }

    public string Function { get; }

    public object?[] Args { get; }
}
