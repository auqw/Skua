using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Skua.Core.Services;

public class LogService : ObservableRecipient, ILogService, IDisposable
{
    private readonly DebugListener _debugListener;
    private const int MaxLogEntries = 10000;
    private readonly object _debugLock = new();
    private readonly object _scriptLock = new();
    private readonly object _flashLock = new();

    public LogService()
    {
        _debugListener = new DebugListener(this);
        Trace.Listeners.Add(_debugListener);
        Messenger.Register<LogService, FlashErrorMessage>(this, LogFlashError);
    }

    private void LogFlashError(LogService recipient, FlashErrorMessage message)
    {
        Exception ex = message.Exception;
        // Failed calls into Flash (a COMException, or an error never thrown) have no useful managed stack.
        string error = ex is COMException || ex.StackTrace is null ? $"{ex.GetType().Name}: {ex.Message}" : ex.ToString();
        recipient.FlashLog($"{message.Function} Args[{message.Args.Length}] {(message.Args.Length > 0 ? $"= {{{string.Join(",", message.Args.Select(a => a?.ToString()))}}}" : string.Empty)}\n{error}");
    }

    private readonly List<string> _debugLogs = new();
    private readonly List<string> _scriptLogs = new();
    private readonly List<string> _flashLogs = new();

    public void DebugLog(string message)
    {
        lock (_debugLock)
        {
            if (_debugLogs.Count >= MaxLogEntries)
                _debugLogs.RemoveAt(0);
            _debugLogs.Add(message);
        }
        Messenger.Send(new LogsChangedMessage(LogType.Debug));
        Messenger.Send(new AddLogMessage(LogType.Debug, message));
    }

    public void FlashLog(string? message)
    {
        if (message is null)
            return;
        lock (_flashLock)
        {
            if (_flashLogs.Count >= MaxLogEntries)
                _flashLogs.RemoveAt(0);
            _flashLogs.Add(message);
        }
        Messenger.Send(new LogsChangedMessage(LogType.Flash));
        Messenger.Send(new AddLogMessage(LogType.Flash, message));
    }

    public void ScriptLog(string? message)
    {
        if (message is null)
            return;
        lock (_scriptLock)
        {
            if (_scriptLogs.Count >= MaxLogEntries)
                _scriptLogs.RemoveAt(0);
            _scriptLogs.Add(message);
        }
        Messenger.Send(new LogsChangedMessage(LogType.Script));
        Messenger.Send(new AddLogMessage(LogType.Script, message));
    }

    public void ClearLog(LogType logType)
    {
        switch (logType)
        {
            case LogType.Debug:
                lock (_debugLock)
                    _debugLogs.Clear();
                break;

            case LogType.Script:
                lock (_scriptLock)
                    _scriptLogs.Clear();
                break;

            case LogType.Flash:
                lock (_flashLock)
                    _flashLogs.Clear();
                break;
        }
        Messenger.Send(new LogsChangedMessage(logType));
    }

    public List<string> GetLogs(LogType logType)
    {
        return logType switch
        {
            LogType.Debug => new List<string>(_debugLogs),
            LogType.Script => new List<string>(_scriptLogs),
            LogType.Flash => new List<string>(_flashLogs),
            _ => new()
        };
    }

    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Remove the trace listener
            if (_debugListener != null)
            {
                Trace.Listeners.Remove(_debugListener);
            }

            // Unregister from messenger
            Messenger.UnregisterAll(this);

            // Clear log collections
            _debugLogs.Clear();
            _scriptLogs.Clear();
            _flashLogs.Clear();
        }

        _disposed = true;
    }

    ~LogService()
    {
        Dispose(false);
    }
}

public class DebugListener : TraceListener
{
    private readonly ILogService _logService;

    public DebugListener(ILogService logService)
    {
        _logService = logService;
    }

    public override void Write(string? message)
    {
        if (message is null)
            return;
        _logService.DebugLog(message);
    }

    public override void WriteLine(string? message)
    {
        if (message is null)
            return;
        _logService.DebugLog(message);
    }
}