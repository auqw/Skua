using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Threading;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Models;
using Skua.Core.Utils;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#if IS_WINDOWS
using System.Runtime.InteropServices;
#endif

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
    private bool _isMainWindowActive;
    private bool _disposed;
#if IS_WINDOWS
    private IntPtr _keyboardHook = IntPtr.Zero;
    private LowLevelKeyboardProc? _keyboardProc;
#endif

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

            _bindings.Add(new HotKeyBinding(binding, command, parsed.Key, parsed.Modifiers, TryToVirtualKey(parsed.Key)));
        }

#if IS_WINDOWS
        EnsureWindowsKeyboardHook();
#endif
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
            else if (string.Equals(key, "TogglePerformanceStrip", StringComparison.Ordinal) && !usedGestures.Contains("F7"))
                gesture = "F7";

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
            {
                _mainWindow.RemoveHandler(InputElement.KeyDownEvent, MainWindowOnKeyDown);
                _mainWindow.Activated -= MainWindowOnActivated;
                _mainWindow.Deactivated -= MainWindowOnDeactivated;
            }

            _mainWindow = desktop.MainWindow;
            _mainWindow.RemoveHandler(InputElement.KeyDownEvent, MainWindowOnKeyDown);
            _mainWindow.AddHandler(InputElement.KeyDownEvent, MainWindowOnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            _mainWindow.Activated += MainWindowOnActivated;
            _mainWindow.Deactivated += MainWindowOnDeactivated;
            _isMainWindowActive = _mainWindow.IsActive;
        }
    }

    private void MainWindowOnActivated(object? sender, EventArgs e) => _isMainWindowActive = true;

    private void MainWindowOnDeactivated(object? sender, EventArgs e) => _isMainWindowActive = false;

    private void MainWindowOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_bindings.Count == 0)
            return;

        KeyModifiers modifiers = e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift);
        foreach (HotKeyBinding binding in _bindings)
        {
            if (binding.Key != e.Key || binding.Modifiers != modifiers)
                continue;

            if (ExecuteBinding(binding))
                e.Handled = true;
            return;
        }
    }

#if IS_WINDOWS
    private void EnsureWindowsKeyboardHook()
    {
        if (_keyboardHook != IntPtr.Zero)
            return;

        _keyboardProc = KeyboardHookCallback;
        _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, IntPtr.Zero, 0);
        if (_keyboardHook == IntPtr.Zero)
        {
            int error = Marshal.GetLastWin32Error();
            Console.Error.WriteLine($"HotKeyService keyboard hook install failed (Win32: {error}). Falling back to Avalonia KeyDown routing only.");
        }
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            int vkCode = Marshal.ReadInt32(lParam);
            KeyModifiers modifiers = GetCurrentModifiers();

            if (_isMainWindowActive && TryExecuteVirtualBinding(vkCode, modifiers))
            {
                // Match WPF behavior: consume hotkey once matched.
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
    }

    private bool TryExecuteVirtualBinding(int vkCode, KeyModifiers modifiers)
    {
        foreach (HotKeyBinding binding in _bindings)
        {
            if (binding.VirtualKey != vkCode || binding.Modifiers != modifiers)
                continue;

            if (!binding.Command.CanExecute(null))
                continue;

            Dispatcher.UIThread.Post(() => ExecuteBinding(binding), DispatcherPriority.Input);
            return true;
        }

        return false;
    }

    private static KeyModifiers GetCurrentModifiers()
    {
        KeyModifiers modifiers = KeyModifiers.None;
        if ((GetKeyState(VK_CONTROL) & 0x8000) != 0)
            modifiers |= KeyModifiers.Control;
        if ((GetKeyState(VK_MENU) & 0x8000) != 0)
            modifiers |= KeyModifiers.Alt;
        if ((GetKeyState(VK_SHIFT) & 0x8000) != 0)
            modifiers |= KeyModifiers.Shift;
        return modifiers;
    }
#endif

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

    private static int? TryToVirtualKey(Key key)
    {
        if (key is >= Key.A and <= Key.Z)
            return 'A' + (int)(key - Key.A);
        if (key is >= Key.D0 and <= Key.D9)
            return 0x30 + (int)(key - Key.D0);
        if (key is >= Key.NumPad0 and <= Key.NumPad9)
            return 0x60 + (int)(key - Key.NumPad0);
        if (key is >= Key.F1 and <= Key.F24)
            return 0x70 + (int)(key - Key.F1);

        return key switch
        {
            Key.Enter => 0x0D,
            Key.Tab => 0x09,
            Key.Escape => 0x1B,
            Key.Space => 0x20,
            Key.Back => 0x08,
            Key.Delete => 0x2E,
            Key.Insert => 0x2D,
            Key.Home => 0x24,
            Key.End => 0x23,
            Key.PageUp => 0x21,
            Key.PageDown => 0x22,
            Key.Left => 0x25,
            Key.Up => 0x26,
            Key.Right => 0x27,
            Key.Down => 0x28,
            _ => null
        };
    }

    private static bool ExecuteBinding(HotKeyBinding binding)
    {
        if (!binding.Command.CanExecute(null))
            return false;

        binding.Command.Execute(null);
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_mainWindow is not null)
        {
            _mainWindow.RemoveHandler(InputElement.KeyDownEvent, MainWindowOnKeyDown);
            _mainWindow.Activated -= MainWindowOnActivated;
            _mainWindow.Deactivated -= MainWindowOnDeactivated;
        }

#if IS_WINDOWS
        if (_keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHook);
            _keyboardHook = IntPtr.Zero;
        }
#endif

        _bindings.Clear();
        GC.SuppressFinalize(this);
    }

    private sealed record HotKeyBinding(string Binding, IRelayCommand Command, Key Key, KeyModifiers Modifiers, int? VirtualKey);

    private sealed record KeyGestureBinding(Key Key, KeyModifiers Modifiers);

#if IS_WINDOWS
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);
#endif
}
