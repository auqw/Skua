using CommunityToolkit.Mvvm.DependencyInjection;
using Skua.Core.Interfaces;
using Skua.Core.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Skua.WPF.UserControls;

/// <summary>
/// Interaction logic for GameContainerUserControl.xaml
/// </summary>
public partial class GameContainerUserControl : System.Windows.Controls.UserControl
{
    private IScriptInterface _bot;
    private UnityGameLauncher? _gameLauncher;
    private System.Windows.Forms.Panel? _hostPanel;

    public GameContainerUserControl()
    {
        InitializeComponent();
        _bot = Ioc.Default.GetRequiredService<IScriptInterface>();
        gameContainer.Visibility = Visibility.Hidden;
        
        // Create a Forms Panel to host the game
        _hostPanel = new System.Windows.Forms.Panel
        {
            Dock = System.Windows.Forms.DockStyle.Fill,
            BackColor = System.Drawing.Color.Black
        };
        gameContainer.Child = _hostPanel;
        
        // Add mouse event handlers to help with focus
        _hostPanel.MouseDown += (s, args) => FocusGameWindow();
        _hostPanel.Click += (s, args) => FocusGameWindow();
        
        Loaded += GameContainer_Loaded;
        SizeChanged += GameContainer_SizeChanged;
    }

    private void FocusGameWindow()
    {
        if (_gameLauncher != null && _gameLauncher.GameWindowHandle != IntPtr.Zero)
        {
            SetFocus(_gameLauncher.GameWindowHandle);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    private async void GameContainer_Loaded(object sender, RoutedEventArgs e)
    {
        _gameLauncher = new UnityGameLauncher();
        _gameLauncher.GameLoaded += OnGameLoaded;

        // Launch the Unity game
        // TODO: Add settings to specify game path or use Steam
        bool launched = await _gameLauncher.LaunchGameAsync();

        if (!launched)
        {
            LoadingBar.Visibility = Visibility.Hidden;
            System.Windows.MessageBox.Show("Failed to launch Unity AQW game. Make sure the game is installed via Steam.", 
                          "Game Launch Error", 
                          MessageBoxButton.OK, 
                          MessageBoxImage.Error);
        }

        Loaded -= GameContainer_Loaded;
    }

    private void OnGameLoaded(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(async () =>
        {
            // Wait for container to be fully rendered
            await Task.Delay(500);
            
            // Get the Panel's window handle
            IntPtr hostHandle = _hostPanel!.Handle;
            
            if (_gameLauncher!.EmbedGameWindow(hostHandle))
            {
                LoadingBar.Visibility = Visibility.Hidden;
                gameContainer.Visibility = Visibility.Visible;
                
                // Wait a moment for visibility to update
                await Task.Delay(100);
                
                // Force update layout
                gameContainer.UpdateLayout();
                _hostPanel.Refresh();
                
                // Get actual size from the Panel's client size (more accurate)
                int width = _hostPanel.ClientSize.Width;
                int height = _hostPanel.ClientSize.Height;
                
                _gameLauncher.ResizeGameWindow(width, height);
                
                // Force another resize after a short delay to ensure it sticks
                await Task.Delay(200);
                _gameLauncher.ResizeGameWindow(width, height);
            }
            else
            {
                System.Windows.MessageBox.Show("Failed to embed Unity game window.", 
                              "Embedding Error", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Error);
            }
        });
    }

    private void GameContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _gameLauncher?.ResizeGameWindow(
            (int)gameContainer.ActualWidth,
            (int)gameContainer.ActualHeight
        );
    }
}
