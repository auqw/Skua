using Avalonia.Threading;
using System;

namespace Skua.App.Avalonia.Services;

public class DispatcherService : Skua.Core.Interfaces.IDispatcherService
{
    public void Invoke(Action action)
    {
        Dispatcher.UIThread.Post(action);
    }
}
