using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Skua.App.Avalonia.Flash;

public interface IFlashHostTransport : IAsyncDisposable
{
    event EventHandler<FlashHostCallbackEventArgs>? CallbackReceived;

    Task StartAsync(CancellationToken cancellationToken = default);
    Task<FlashHostResponse> SendAsync(FlashHostRequest request, CancellationToken cancellationToken = default);
}

public sealed record FlashHostRequest(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("function")] string? Function = null,
    [property: JsonPropertyName("xml")] string? Xml = null);

public sealed record FlashHostResponse(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("ok")] bool Ok,
    [property: JsonPropertyName("xml")] string? Xml = null,
    [property: JsonPropertyName("error")] string? Error = null);

public sealed class FlashHostCallbackEventArgs : EventArgs
{
    public FlashHostCallbackEventArgs(string function, object?[] args)
    {
        Function = function;
        Args = args;
    }

    [JsonPropertyName("function")]
    public string Function { get; }

    [JsonPropertyName("args")]
    public object?[] Args { get; }
}
