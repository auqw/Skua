using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Skua.Manager.Avalonia.Services;

public class FileDialogService : IFileDialogService
{
    private const string DefaultFilter = "Text Files (*.txt)|*.txt";

    public string? OpenFile() => OpenFile(ClientFileSources.SkuaDIR, string.Empty);

    public string? OpenFile(string filters) => OpenFile(ClientFileSources.SkuaDIR, filters);

    public string? OpenFile(string initialDirectory, string filters)
    {
        IStorageProvider? provider = GetStorageProvider();
        if (provider == null)
            return null;

        FilePickerOpenOptions options = new()
        {
            AllowMultiple = false,
            SuggestedStartLocation = ResolveFolder(provider, initialDirectory),
            FileTypeFilter = ParseFilters(filters)
        };

        IReadOnlyList<IStorageFile> files = provider.OpenFilePickerAsync(options).GetAwaiter().GetResult();
        return files.FirstOrDefault()?.TryGetLocalPath();
    }

    public string? OpenFolder() => OpenFolder(ClientFileSources.SkuaDIR);

    public string? OpenFolder(string initialDirectory)
    {
        IStorageProvider? provider = GetStorageProvider();
        if (provider == null)
            return null;

        FolderPickerOpenOptions options = new()
        {
            AllowMultiple = false,
            SuggestedStartLocation = ResolveFolder(provider, initialDirectory)
        };

        IReadOnlyList<IStorageFolder> folders = provider.OpenFolderPickerAsync(options).GetAwaiter().GetResult();
        return folders.FirstOrDefault()?.TryGetLocalPath();
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
        IStorageProvider? provider = GetStorageProvider();
        if (provider == null)
            return null;

        FilePickerSaveOptions options = new()
        {
            SuggestedStartLocation = ResolveFolder(provider, initialDirectory),
            FileTypeChoices = ParseFilters(filters)
        };

        IStorageFile? file = provider.SaveFilePickerAsync(options).GetAwaiter().GetResult();
        return file?.TryGetLocalPath();
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

    private static IStorageProvider? GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;
        return desktop.MainWindow?.StorageProvider;
    }

    private static IStorageFolder? ResolveFolder(IStorageProvider provider, string initialDirectory)
    {
        if (string.IsNullOrWhiteSpace(initialDirectory))
            return null;
        return provider.TryGetFolderFromPathAsync(initialDirectory).GetAwaiter().GetResult();
    }

    private static List<FilePickerFileType> ParseFilters(string filters)
    {
        string input = string.IsNullOrWhiteSpace(filters) ? DefaultFilter : filters;
        List<FilePickerFileType> results = [];
        string[] parts = input.Split('|', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            string label = parts[i];
            string[] patterns = parts[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            results.Add(new FilePickerFileType(label) { Patterns = patterns });
        }
        if (results.Count == 0)
            results.Add(new FilePickerFileType("All files") { Patterns = ["*.*"] });
        return results;
    }
}
