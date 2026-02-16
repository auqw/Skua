using Skua.Core.Interfaces;
using System;
using System.Xml.Linq;

namespace Skua.App.Avalonia.Flash;

public class StubFlashUtil : IFlashUtil
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public event FlashCallHandler? FlashCall;
    public void InitializeFlash()
    {
        throw new NotImplementedException();
    }

    public string? Call(string function, params object[] args)
    {
        throw new NotImplementedException();
    }

    public T? Call<T>(string function, params object[] args)
    {
        throw new NotImplementedException();
    }

    public object? Call(string function, Type type, params object[] args)
    {
        throw new NotImplementedException();
    }

    public object FromFlashXml(XElement el)
    {
        throw new NotImplementedException();
    }

    public IFlashObject<T> CreateFlashObject<T>(string path)
    {
        throw new NotImplementedException();
    }
}