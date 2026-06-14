using System;
using System.IO;

namespace Skua.App.Avalonia.Flash;

public sealed record ElectronFlashHostOptions(
    string ElectronPath,
    string HostDirectory,
    string SwfPath,
    string FlashPluginPath)
{
    public static ElectronFlashHostOptions FromEnvironment()
    {
        string baseDir = AppContext.BaseDirectory;
        string hostDir = Environment.GetEnvironmentVariable("SKUA_FLASH_HOST_DIR")
            ?? Path.Combine(baseDir, "linux-flash-host");
        string electronPath = Environment.GetEnvironmentVariable("SKUA_ELECTRON_BIN")
            ?? "electron";
        string swfPath = Environment.GetEnvironmentVariable("SKUA_SWF_PATH")
            ?? Path.Combine(baseDir, "skua.swf");
        string pluginPath = Environment.GetEnvironmentVariable("SKUA_FLASH_PLUGIN")
            ?? Path.Combine(hostDir, "plugins", "libpepflashplayer.so");

        return new ElectronFlashHostOptions(electronPath, hostDir, swfPath, pluginPath);
    }

    public void Validate()
    {
        if (!CommandExists(ElectronPath))
            throw new InvalidOperationException($"Electron binary not found. Set SKUA_ELECTRON_BIN to an Electron 8-compatible executable. Current value: {ElectronPath}");

        if (!Directory.Exists(HostDirectory))
            throw new InvalidOperationException($"Linux Flash host directory not found. Set SKUA_FLASH_HOST_DIR. Current value: {HostDirectory}");

        if (!File.Exists(SwfPath))
            throw new InvalidOperationException($"skua.swf not found. Build/copy the AS3 client or set SKUA_SWF_PATH. Current value: {SwfPath}");

        if (!File.Exists(FlashPluginPath))
            throw new InvalidOperationException($"PPAPI Flash plugin not found. Set SKUA_FLASH_PLUGIN to libpepflashplayer.so. Current value: {FlashPluginPath}");
    }

    private static bool CommandExists(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        if (Path.IsPathRooted(command) || command.Contains(Path.DirectorySeparatorChar))
            return File.Exists(command);

        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            return false;

        foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(directory, command);
            if (File.Exists(candidate))
                return true;
        }

        return false;
    }
}
