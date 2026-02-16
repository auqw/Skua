using Skua.Core.Interfaces;
using System.Collections.Generic;

namespace Skua.App.Avalonia.Services;

public class StubFileDialogService : IFileDialogService
{
    public string? OpenFile()
    {
        throw new System.NotImplementedException();
    }

    public string? OpenFile(string filters)
    {
        throw new System.NotImplementedException();
    }

    public string? OpenFile(string initialDirectory, string filters)
    {
        throw new System.NotImplementedException();
    }

    public string? OpenFolder()
    {
        throw new System.NotImplementedException();
    }

    public string? OpenFolder(string initialDirectory)
    {
        throw new System.NotImplementedException();
    }

    public IEnumerable<string>? OpenText()
    {
        throw new System.NotImplementedException();
    }

    public string? Save()
    {
        throw new System.NotImplementedException();
    }

    public string? Save(string filters)
    {
        throw new System.NotImplementedException();
    }

    public string? Save(string initialDirectory, string filters)
    {
        throw new System.NotImplementedException();
    }

    public void SaveText(string contents)
    {
        throw new System.NotImplementedException();
    }

    public void SaveText(IEnumerable<string> contents)
    {
        throw new System.NotImplementedException();
    }
}