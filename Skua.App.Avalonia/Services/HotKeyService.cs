using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Models;
using Skua.Core.Utils;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Skua.App.Avalonia.Services;

public class HotKeyService : IHotKeyService, IDisposable
{
    public HotKeyService(Dictionary<string, IRelayCommand> hotKeys, ISettingsService settingsService, IDecamelizer decamelizer)
    {
        _hotKeys = hotKeys;
        _settingsService = settingsService;
        _decamelizer = decamelizer;
    }

    private readonly Dictionary<string, IRelayCommand> _hotKeys;
    private readonly ISettingsService _settingsService;
    private readonly IDecamelizer _decamelizer;
    private readonly List<HotKeyBinding> _bindings = [];
    private Window? _mainWindow;
    private bool _disposed;

    public void Reload()
    {
        StringCollection hotkeys = _settingsService.Get<StringCollection>("HotKeys") ?? new StringCollection();
        EnsureAllBindingsExist(hotkeys);
        _settingsService.Set("HotKeys", hotkeys);

        AttachMainWindow();
        _bindings.Clear();

        foreach (string hk in hotkeys)
        {
            if (string.IsNullOrWhiteSpace(hk))
                continue;

            string[] split = hk.Split('|');
            string binding = split[0];
            if (!_hotKeys.TryGetValue(binding, out IRelayCommand? command))
                continue;

            if (split.Length < 2 || string.IsNullOrWhiteSpace(split[1]))
                continue;

            KeyGestureBinding? parsed = ParseGesture(split[1]);
            if (parsed is null)
            {
                StrongReferenceMessenger.Default.Send<HotKeyErrorMessage>(new(binding));
                continue;
            }

            _bindings.Add(new HotKeyBinding(binding, command, parsed.Key, parsed.Modifiers));
        }
    }

    public List<T> GetHotKeys<T>()
        where T : IHotKey, new()
    {
        StringCollection hotkeys = _settingsService.Get<StringCollection>("HotKeys") ?? new StringCollection();
        EnsureAllBindingsExist(hotkeys);
        _settingsService.Set("HotKeys", hotkeys);

        List<T> parsed = [];
        foreach (string hk in hotkeys)
        {
            if (string.IsNullOrWhiteSpace(hk))
                continue;

            string[] split = hk.Split('|');
            string binding = split[0];
            string gesture = split.Length > 1 ? split[1] : string.Empty;
            parsed.Add(new()
            {
                Binding = binding,
                Title = _decamelizer.Decamelize(binding, null),
                KeyGesture = gesture
            });
        }

        return parsed;
    }

    public HotKey? ParseToHotKey(string keyGesture)
    {
        if (string.IsNullOrWhiteSpace(keyGesture))
            return null;

        KeyGestureBinding? parsed = ParseGesture(keyGesture);
        if (parsed is null)
            return null;

        string normalized = parsed.Key.ToString();
        return new HotKey(
            normalized,
            parsed.Modifiers.HasFlag(KeyModifiers.Control),
            parsed.Modifiers.HasFlag(KeyModifiers.Alt),
            parsed.Modifiers.HasFlag(KeyModifiers.Shift));
    }

    private void EnsureAllBindingsExist(StringCollection hotkeys)
    {
        HashSet<string> existing = [];
        HashSet<string> usedGestures = new(StringComparer.OrdinalIgnoreCase);

        foreach (string hk in hotkeys)
        {
            if (string.IsNullOrWhiteSpace(hk))
                continue;

            string[] split = hk.Split('|');
            if (split.Length > 0 && !string.IsNullOrWhiteSpace(split[0]))
                existing.Add(split[0]);
            if (split.Length > 1 && !string.IsNullOrWhiteSpace(split[1]))
                usedGestures.Add(split[1]);
        }

        foreach (string key in _hotKeys.Keys)
        {
            if (existing.Contains(key))
                continue;

            string gesture = string.Empty;
            if (string.Equals(key, "ToggleLagKiller", StringComparison.Ordinal) && !usedGestures.Contains("F6"))
                gesture = "F6";

            hotkeys.Add($"{key}|{gesture}");
        }
    }

    private void AttachMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null)
            return;

        if (!ReferenceEquals(_mainWindow, desktop.MainWindow))
        {
            if (_mainWindow is not null)
                _mainWindow.KeyDown -= MainWindowOnKeyDown;

            _mainWindow = desktop.MainWindow;
            _mainWindow.KeyDown -= MainWindowOnKeyDown;
            _mainWindow.KeyDown += MainWindowOnKeyDown;
        }
    }

    private void MainWindowOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_bindings.Count == 0)
            return;

        KeyModifiers modifiers = e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift);
        foreach (HotKeyBinding binding in _bindings)
        {
            if (binding.Key != e.Key || binding.Modifiers != modifiers)
                continue;

            if (!binding.Command.CanExecute(null))
                continue;

            binding.Command.Execute(null);
            e.Handled = true;
            return;
        }
    }

    private static KeyGestureBinding? ParseGesture(string gesture)
    {
        string[] parts = gesture
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return null;

        KeyModifiers modifiers = KeyModifiers.None;
        string? keyPart = null;
        foreach (string part in parts)
        {
            if (part.Equals("ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("ctl", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= KeyModifiers.Control;
                continue;
            }
            if (part.Equals("alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= KeyModifiers.Alt;
                continue;
            }
            if (part.Equals("shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= KeyModifiers.Shift;
                continue;
            }

            keyPart = part;
        }

        if (string.IsNullOrWhiteSpace(keyPart))
            return null;

        string normalized = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(keyPart.ToLowerInvariant());
        if (normalized.Length == 1 && char.IsDigit(normalized[0]))
            normalized = $"D{normalized}";

        if (!Enum.TryParse(normalized, true, out Key key) || key == Key.None)
            return null;

        return new KeyGestureBinding(key, modifiers);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_mainWindow is not null)
            _mainWindow.KeyDown -= MainWindowOnKeyDown;

        _bindings.Clear();
        GC.SuppressFinalize(this);
    }

    private sealed record HotKeyBinding(string Binding, IRelayCommand Command, Key Key, KeyModifiers Modifiers);

    private sealed record KeyGestureBinding(Key Key, KeyModifiers Modifiers);
}
