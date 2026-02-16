using Skua.Core.Interfaces;
using Skua.Core.Models;
using System.Collections.Generic;
using System.IO;
using WinForms = System.Windows.Forms;

namespace Skua.App.Avalonia.Services;

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
