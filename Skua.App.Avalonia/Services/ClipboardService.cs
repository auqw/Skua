using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Skua.Core.Interfaces;
using System.Collections.Concurrent;

namespace Skua.App.Avalonia.Services;

public class ClipboardService : IClipboardService
{
    private readonly ConcurrentDictionary<string, object> _data = new();

    public void SetText(string text)
    {
        _ = GetClipboard()?.SetTextAsync(text);
    }

    public void SetData(string format, object data)
    {
        _data[format] = data;
    }

    public object GetData(string format)
    {
        return _data.TryGetValue(format, out object? value) ? value : string.Empty;
    }

    public string GetText()
    {
        return GetClipboard()?.GetTextAsync().GetAwaiter().GetResult() ?? string.Empty;
    }

    private static global::Avalonia.Input.Platform.IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;
        return TopLevel.GetTopLevel(desktop.MainWindow)?.Clipboard;
    }
}
