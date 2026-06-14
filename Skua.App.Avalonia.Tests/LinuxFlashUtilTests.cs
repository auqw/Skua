using Skua.App.Avalonia.Flash;
using Xunit;

namespace Skua.App.Avalonia.Tests;

public sealed class LinuxFlashUtilTests
{
    [Fact]
    public void InitializeFlash_StartsHostClient()
    {
        FakeFlashHostClient client = new();
        using FlashUtil util = new(client);

        util.InitializeFlash();

        Assert.True(client.Started);
    }

    [Fact]
    public void InitializeFlash_LoadsClientAfterHostStarts()
    {
        FakeFlashHostClient client = new();
        using FlashUtil util = new(client);

        util.InitializeFlash();

        Assert.Contains("loadClient", client.Calls.Select(call => call.Function));
    }

    [Fact]
    public void HostFlashCall_RaisesIFlashUtilEvent()
    {
        FakeFlashHostClient client = new();
        using FlashUtil util = new(client);
        string? name = null;
        object[]? args = null;
        util.FlashCall += (function, values) =>
        {
            name = function;
            args = values;
        };

        client.RaiseFlashCall("loaded", "x", 2);

        Assert.Equal("loaded", name);
        Assert.Equal(new object[] { "x", 2 }, args);
    }

    [Fact]
    public void Call_DelegatesToHostClient()
    {
        FakeFlashHostClient client = new() { Result = "ok" };
        using FlashUtil util = new(client);

        string? result = util.Call("loadClient", "arg");

        Assert.Equal("ok", result);
        Assert.Equal("loadClient", client.LastFunction);
        Assert.Equal(new object?[] { "arg" }, client.LastArgs);
    }

    [Fact]
    public void CallGeneric_ConvertsHostResult()
    {
        FakeFlashHostClient client = new() { Result = "7" };
        using FlashUtil util = new(client);

        int result = util.Call<int>("getCount");

        Assert.Equal(7, result);
    }

    private sealed class FakeFlashHostClient : IFlashHostClient
    {
        public event EventHandler<FlashHostCallEventArgs>? FlashCalled;
        public bool Started { get; private set; }
        public string? Result { get; init; }
        public string? LastFunction { get; private set; }
        public object?[]? LastArgs { get; private set; }
        public List<(string Function, object?[] Args)> Calls { get; } = new();

        public void Start()
        {
            Started = true;
        }

        public string? Call(string function, params object?[] args)
        {
            LastFunction = function;
            LastArgs = args;
            Calls.Add((function, args));
            return Result;
        }

        public void RaiseFlashCall(string function, params object?[] args)
        {
            FlashCalled?.Invoke(this, new FlashHostCallEventArgs(function, args));
        }

        public void Dispose()
        {
        }
    }
}
