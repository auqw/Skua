using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using System;
using System.Threading.Tasks;

namespace Skua.App.Avalonia.Services;

public sealed class SkuaStartupHandler : IDisposable
{
    private readonly IScriptInterface _bot;
    private readonly ISettingsService _settingsService;
    private readonly IThemeService _themeService;
    private readonly StartupOptions _options;
    private readonly bool _optionsParsed;
    private bool _clientLoadRequested;
    private bool _loginTriggered;
    private bool _disposed;

    public SkuaStartupHandler(IScriptInterface bot, ISettingsService settingsService, IThemeService themeService)
    {
        _bot = bot;
        _settingsService = settingsService;
        _themeService = themeService;
        (_optionsParsed, _options) = StartupOptions.Parse(Environment.GetCommandLineArgs());
    }

    public void Execute()
    {
        _bot.Flash.FlashCall -= LoadGame;
        _bot.Flash.FlashCall += LoadGame;
        _bot.Flash.FlashCall -= Login;

        if (!_optionsParsed)
            return;

        if (!string.IsNullOrWhiteSpace(_options.GitHubToken))
            _settingsService.Set("UserGitHubToken", _options.GitHubToken!);

        if (!string.IsNullOrWhiteSpace(_options.UseTheme) && !string.Equals(_options.UseTheme, "no-theme", StringComparison.OrdinalIgnoreCase))
            _themeService.SetCurrentTheme(_options.UseTheme!);

        if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
        {
            _bot.Flash.FlashCall += Login;
            _bot.Servers.SetLoginInfo(_options.Username!, _options.Password!);
        }
    }

    private void LoadGame(string function, params object[] args)
    {
        if (function != "requestLoadGame" || _clientLoadRequested)
            return;

        _clientLoadRequested = true;
        _bot.Flash.Call("loadClient");
        _bot.Flash.FlashCall -= LoadGame;
    }

    private void Login(string function, params object[] args)
    {
        if (_loginTriggered || function != "loaded")
            return;

        _loginTriggered = true;
        _bot.Flash.FlashCall -= Login;

        Task.Factory.StartNew(() =>
        {
            _bot.Servers.Relogin(string.IsNullOrWhiteSpace(_options.Server) ? "Twilly" : _options.Server!);
            if (!string.IsNullOrWhiteSpace(_options.Script))
                StrongReferenceMessenger.Default.Send<StartScriptMessage, int>(new(_options.Script!), (int)MessageChannels.ScriptStatus);
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _bot.Flash.FlashCall -= LoadGame;
        _bot.Flash.FlashCall -= Login;
        GC.SuppressFinalize(this);
    }

    private sealed class StartupOptions
    {
        public string? Username { get; private set; }
        public string? Password { get; private set; }
        public string? Server { get; private set; } = "Twilly";
        public string? Script { get; private set; }
        public string? UseTheme { get; private set; }
        public string? GitHubToken { get; private set; }

        public static (bool Parsed, StartupOptions Options) Parse(string[] args)
        {
            StartupOptions options = new();
            if (args.Length <= 1)
                return (true, options);

            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                if (!arg.StartsWith("-"))
                    continue;

                static bool NeedsValue(string value) => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("-");

                string? NextValue()
                {
                    if (i + 1 >= args.Length)
                        return null;
                    string value = args[i + 1];
                    if (!NeedsValue(value))
                        return null;
                    i++;
                    return value;
                }

                switch (arg)
                {
                    case "-u":
                    case "--user":
                        options.Username = NextValue();
                        break;
                    case "-p":
                    case "--password":
                        options.Password = NextValue();
                        break;
                    case "-s":
                    case "--server":
                        options.Server = NextValue() ?? options.Server;
                        break;
                    case "--run-script":
                        options.Script = NextValue();
                        break;
                    case "--use-theme":
                        options.UseTheme = NextValue();
                        break;
                    case "--gh-token":
                        options.GitHubToken = NextValue();
                        break;
                }
            }

            return (true, options);
        }
    }
}
