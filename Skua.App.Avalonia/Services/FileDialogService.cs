using Skua.Core.Interfaces;
using Skua.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Skua.App.Avalonia.Services;

#if IS_WINDOWS
using WinForms = System.Windows.Forms;
public class FileDialogService : IFileDialogService
{
    private const string DefaultFilter = "Text Files (*.txt)|*.txt";

    public string? OpenFile() => OpenFile(ClientFileSources.SkuaDIR, "All Files (*.*)|*.*");

    public string? OpenFile(string filters) => OpenFile(ClientFileSources.SkuaDIR, filters);

    public string? OpenFile(string initialDirectory, string filters)
    {
        using WinForms.OpenFileDialog dialog = new()
        {
            InitialDirectory = initialDirectory,
            Filter = string.IsNullOrWhiteSpace(filters) ? "All Files (*.*)|*.*" : filters
        };

        return dialog.ShowDialog() == WinForms.DialogResult.OK ? dialog.FileName : null;
    }

    public string? OpenFolder() => OpenFolder(ClientFileSources.SkuaDIR);

    public string? OpenFolder(string initialDirectory)
    {
        using WinForms.FolderBrowserDialog dialog = new()
        {
            Description = "Select the folder.",
            UseDescriptionForTitle = true,
            InitialDirectory = initialDirectory
        };

        return dialog.ShowDialog() == WinForms.DialogResult.OK ? dialog.SelectedPath : null;
    }

    public IEnumerable<string>? OpenText()
    {
        string? path = OpenFile(ClientFileSources.SkuaDIR, DefaultFilter);
        return string.IsNullOrWhiteSpace(path) ? null : File.ReadAllLines(path);
    }

    public string? Save() => Save(ClientFileSources.SkuaDIR, DefaultFilter);

    public string? Save(string filters) => Save(ClientFileSources.SkuaDIR, filters);

    public string? Save(string initialDirectory, string filters)
    {
        using WinForms.SaveFileDialog dialog = new()
        {
            InitialDirectory = initialDirectory,
            Filter = string.IsNullOrWhiteSpace(filters) ? DefaultFilter : filters
        };

        return dialog.ShowDialog() == WinForms.DialogResult.OK ? dialog.FileName : null;
    }

    public void SaveText(string contents)
    {
        string? path = Save();
        if (!string.IsNullOrWhiteSpace(path))
            File.WriteAllText(path, contents);
    }

    public void SaveText(IEnumerable<string> contents)
    {
        string? path = Save();
        if (!string.IsNullOrWhiteSpace(path))
            File.WriteAllLines(path, contents);
    }
}
#else
public class FileDialogService : IFileDialogService
{
    private const string DefaultFilter = "Text Files (*.txt)|*.txt";

    public string? OpenFile() => OpenFile(ClientFileSources.SkuaDIR, "All Files (*.*)|*.*");

    public string? OpenFile(string filters) => OpenFile(ClientFileSources.SkuaDIR, filters);

    public string? OpenFile(string initialDirectory, string filters)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && TryFindExecutable("zenity") is string zenity)
        {
            List<string> args = ["--file-selection"];
            AddInitialPath(args, initialDirectory);
            AddZenityFilters(args, filters);
            return RunDialog(zenity, args);
        }

        return null;
    }

    public string? OpenFolder() => OpenFolder(ClientFileSources.SkuaDIR);

    public string? OpenFolder(string initialDirectory)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && TryFindExecutable("zenity") is string zenity)
        {
            List<string> args = ["--file-selection", "--directory"];
            AddInitialPath(args, initialDirectory);
            return RunDialog(zenity, args);
        }

        return null;
    }

    public IEnumerable<string>? OpenText()
    {
        string? path = OpenFile(ClientFileSources.SkuaDIR, DefaultFilter);
        return string.IsNullOrWhiteSpace(path) ? null : File.ReadAllLines(path);
    }

    public string? Save() => Save(ClientFileSources.SkuaDIR, DefaultFilter);

    public string? Save(string filters) => Save(ClientFileSources.SkuaDIR, filters);

    public string? Save(string initialDirectory, string filters)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && TryFindExecutable("zenity") is string zenity)
        {
            List<string> args = ["--file-selection", "--save", "--confirm-overwrite"];
            AddInitialPath(args, initialDirectory);
            AddZenityFilters(args, filters);
            return RunDialog(zenity, args);
        }

        return null;
    }

    public void SaveText(string contents)
    {
        string? path = Save();
        if (!string.IsNullOrWhiteSpace(path))
            File.WriteAllText(path, contents);
    }

    public void SaveText(IEnumerable<string> contents)
    {
        string? path = Save();
        if (!string.IsNullOrWhiteSpace(path))
            File.WriteAllLines(path, contents);
    }

    private static void AddInitialPath(List<string> args, string initialDirectory)
    {
        if (string.IsNullOrWhiteSpace(initialDirectory))
            return;

        string path = Directory.Exists(initialDirectory)
            ? Path.GetFullPath(initialDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar
            : Path.GetFullPath(initialDirectory);
        args.Add("--filename=" + path);
    }

    private static void AddZenityFilters(List<string> args, string filters)
    {
        if (string.IsNullOrWhiteSpace(filters))
            return;

        string[] parts = filters.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int i = 0; i + 1 < parts.Length; i += 2)
            args.Add("--file-filter=" + parts[i] + " | " + parts[i + 1].Replace(';', ' '));
    }

    private static string? RunDialog(string executable, IReadOnlyList<string> args)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (string arg in args)
            startInfo.ArgumentList.Add(arg);

        using Process? process = Process.Start(startInfo);
        if (process is null)
            return null;

        string output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : null;
    }

    private static string? TryFindExecutable(string name)
    {
        string[] paths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (string path in paths)
        {
            string candidate = Path.Combine(path, name);
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }
}
#endif