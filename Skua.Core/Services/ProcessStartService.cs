using Skua.Core.Interfaces;
using Skua.Core.Models;
using System.ComponentModel;
using System.Diagnostics;

namespace Skua.Core.Services;

public class ProcessStartService : IProcessService
{
    public ProcessStartService(ISettingsService settingsService, IDialogService dialogService)
    {
        _settingsService = settingsService;
        _dialogService = dialogService;
    }

    private readonly string _vscPath = Path.Combine(AppContext.BaseDirectory, "VSCode", "code");
    private readonly string _scriptsPath = ClientFileSources.SkuaScriptsDIR;
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;

    public void OpenLink(string link)
    {
        ProcessStartInfo ps = new(link)
        {
            UseShellExecute = true,
            Verb = "open"
        };
        Process.Start(ps);
    }

    public void OpenVSC()
    {
        if (TryStartLocalVSCode(Quote(_scriptsPath)) || TryStartVSCode(Quote(_scriptsPath)))
            return;

        OpenNotepad(null);
    }

    public void OpenVSC(string path)
    {
        if (TryStartLocalVSCode($"{Quote(_scriptsPath)} {Quote(path)} --reuse-window") || TryStartVSCode($"{Quote(_scriptsPath)} {Quote(path)}"))
            return;

        OpenNotepad(path);
    }

    private bool TryStartLocalVSCode(string arguments)
    {
        if (!_settingsService.Get("UseLocalVSC", false))
            return false;

        try
        {
            Process.Start(_vscPath, arguments);
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private static bool TryStartVSCode(string arguments)
    {
        ProcessStartInfo psi = new("code", arguments)
        {
            UseShellExecute = true,
            CreateNoWindow = true
        };

        try
        {
            Process.Start(psi);
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }

    private void OpenNotepad(string? path)
    {
        try
        {
            Process.Start("notepad", string.IsNullOrEmpty(path) ? string.Empty : Quote(path));
        }
        catch (Win32Exception)
        {
            _dialogService.ShowMessageBox("Neither VSCode nor Notepad could be started.", "Editor not found");
        }
    }

    private static string Quote(string value) => $"\"{value}\"";
}
