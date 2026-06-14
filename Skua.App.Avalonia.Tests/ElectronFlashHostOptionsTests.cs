using Skua.App.Avalonia.Flash;
using Xunit;

namespace Skua.App.Avalonia.Tests;

public sealed class ElectronFlashHostOptionsTests
{
    [Fact]
    public void Validate_RequiresElectronBinary()
    {
        ElectronFlashHostOptions options = new(
            ElectronPath: "/missing/electron",
            HostDirectory: Directory.GetCurrentDirectory(),
            SwfPath: Path.GetTempFileName(),
            FlashPluginPath: Path.GetTempFileName());

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => options.Validate());

        Assert.Contains("Electron", ex.Message);
    }

    [Fact]
    public void Validate_RequiresFlashPlugin()
    {
        string executable = CreateTempExecutable();
        ElectronFlashHostOptions options = new(
            ElectronPath: executable,
            HostDirectory: Directory.GetCurrentDirectory(),
            SwfPath: Path.GetTempFileName(),
            FlashPluginPath: "/missing/libpepflashplayer.so");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => options.Validate());

        Assert.Contains("SKUA_FLASH_PLUGIN", ex.Message);
    }

    [Fact]
    public void Validate_AcceptsExistingPaths()
    {
        string executable = CreateTempExecutable();
        string plugin = Path.GetTempFileName();
        string swf = Path.GetTempFileName();
        ElectronFlashHostOptions options = new(
            ElectronPath: executable,
            HostDirectory: Directory.GetCurrentDirectory(),
            SwfPath: swf,
            FlashPluginPath: plugin);

        options.Validate();
    }

    private static string CreateTempExecutable()
    {
        string path = Path.GetTempFileName();
        File.WriteAllText(path, "#!/bin/sh\nexit 0\n");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return path;
    }
}
