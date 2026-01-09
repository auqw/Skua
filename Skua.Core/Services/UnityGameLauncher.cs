using Skua.Core.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Skua.Core.Services;

public class UnityGameLauncher
{
    // TODO: Replace with actual Steam App ID for AQW Unity
    private const string AQW_UNITY_STEAM_APPID = "2094530";
    
    private Process? _gameProcess;
    private IntPtr _gameWindowHandle;

    #region Win32 API Imports
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int WS_VISIBLE = 0x10000000;
    private const int WS_CHILD = 0x40000000;
    private const int WS_BORDER = 0x00800000;
    private const int WS_CAPTION = 0x00C00000;
    private const int WS_THICKFRAME = 0x00040000;
    private const int WS_EX_DLGMODALFRAME = 0x00000001;
    private const int WS_EX_WINDOWEDGE = 0x00000100;
    private const int WS_EX_CLIENTEDGE = 0x00000200;
    private const int WS_EX_STATICEDGE = 0x00020000;
    private const int WS_DISABLED = 0x08000000;
    private const int SW_SHOW = 5;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    #endregion

    public event EventHandler? GameLoaded;

    public async Task<bool> LaunchGameAsync(string? gamePath = null)
    {
        try
        {
            ProcessStartInfo startInfo;

            if (!string.IsNullOrEmpty(gamePath) && File.Exists(gamePath))
            {
                // Launch directly from executable path
                startInfo = new ProcessStartInfo
                {
                    FileName = gamePath,
                    UseShellExecute = true
                };
            }
            else
            {
                // Launch via Steam protocol
                startInfo = new ProcessStartInfo
                {
                    FileName = $"steam://rungameid/{AQW_UNITY_STEAM_APPID}",
                    UseShellExecute = true
                };
            }

            Process.Start(startInfo);

            // Wait for the game process to start
            await Task.Delay(2000);

            // Try to find the game process
            var processes = Process.GetProcessesByName("AdventureQuest Worlds Infinity");
            if (processes.Length > 0)
            {
                _gameProcess = processes[0];
                
                // Wait for window to be created
                await WaitForWindowAsync(_gameProcess);
                
                GameLoaded?.Invoke(this, EventArgs.Empty);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to launch Unity game: {ex.Message}");
            return false;
        }
    }

    private async Task WaitForWindowAsync(Process process)
    {
        int attempts = 0;
        while (_gameWindowHandle == IntPtr.Zero && attempts < 30)
        {
            await Task.Delay(500);
            process.Refresh();
            _gameWindowHandle = process.MainWindowHandle;
            attempts++;
        }
    }

    public bool EmbedGameWindow(IntPtr parentHandle)
    {
        if (_gameWindowHandle == IntPtr.Zero || parentHandle == IntPtr.Zero)
            return false;

        try
        {
            // Get current window style
            int style = GetWindowLong(_gameWindowHandle, GWL_STYLE);
            
            // Remove: caption, border, thick frame (resize border), disabled
            style &= ~(WS_CAPTION | WS_BORDER | WS_THICKFRAME | WS_DISABLED);
            
            // Add: child window style and visible
            style |= WS_CHILD | WS_VISIBLE;
            
            // Set the new style
            SetWindowLong(_gameWindowHandle, GWL_STYLE, style);
            
            // Ensure window is enabled for input
            EnableWindow(_gameWindowHandle, true);
            
            // Remove extended window borders
            int exStyle = GetWindowLong(_gameWindowHandle, GWL_EXSTYLE);
            exStyle &= ~(WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE);
            SetWindowLong(_gameWindowHandle, GWL_EXSTYLE, exStyle);

            // Set the parent window
            IntPtr result = SetParent(_gameWindowHandle, parentHandle);
            
            if (result == IntPtr.Zero)
            {
                Debug.WriteLine("SetParent failed");
                return false;
            }
            
            // Show the window
            ShowWindow(_gameWindowHandle, SW_SHOW);
            
            // Set focus to the embedded window so it receives input
            SetForegroundWindow(_gameWindowHandle);
            SetFocus(_gameWindowHandle);

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to embed game window: {ex.Message}");
            return false;
        }
    }

    public void ResizeGameWindow(int width, int height)
    {
        if (_gameWindowHandle != IntPtr.Zero)
        {
            // Use SetWindowPos instead of MoveWindow for better control
            bool success = SetWindowPos(_gameWindowHandle, IntPtr.Zero, 0, 0, width, height, SWP_NOZORDER);
            
            if (!success)
            {
                Debug.WriteLine($"SetWindowPos failed for size {width}x{height}");
            }
            else
            {
                Debug.WriteLine($"Resized game window to {width}x{height}");
            }
            
            // Re-focus after resize to ensure input still works
            SetFocus(_gameWindowHandle);
        }
    }

    public void CloseGame()
    {
        try
        {
            _gameProcess?.Kill();
            _gameProcess?.Dispose();
            _gameProcess = null;
            _gameWindowHandle = IntPtr.Zero;
        }
        catch
        {
        }
    }

    public IntPtr GameWindowHandle => _gameWindowHandle;
}
