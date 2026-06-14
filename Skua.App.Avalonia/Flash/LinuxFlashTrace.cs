using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace Skua.App.Avalonia.Flash;

internal static class LinuxFlashTrace
{
    private const int DefaultPreviewLength = 180;
    private static readonly object Gate = new();
    private static readonly ConcurrentDictionary<string, int> QuietEventCounts = new(StringComparer.OrdinalIgnoreCase);
    private static long _sequence;

    public static string TracePath => Environment.GetEnvironmentVariable("SKUA_FLASH_TRACE_PATH")
        ?? "/tmp/skua-linux-flash-trace.log";

    public static bool Enabled => string.Equals(Environment.GetEnvironmentVariable("SKUA_FLASH_TRACE"), "1", StringComparison.Ordinal)
        || string.Equals(Environment.GetEnvironmentVariable("SKUA_FLASH_TRACE_VERBOSE"), "1", StringComparison.Ordinal);

    public static bool PayloadsEnabled => string.Equals(Environment.GetEnvironmentVariable("SKUA_FLASH_TRACE_PAYLOADS"), "1", StringComparison.Ordinal);

    public static void Event(string component, string name, params (string Key, object? Value)[] fields)
    {
        if (!ShouldWrite(component, name))
            return;

        StringBuilder line = new();
        line.Append(DateTimeOffset.Now.ToString("O"));
        line.Append(" seq=").Append(Interlocked.Increment(ref _sequence));
        line.Append(" pid=").Append(Environment.ProcessId);
        line.Append(" tid=").Append(Environment.CurrentManagedThreadId);
        line.Append(' ').Append(component).Append(' ').Append(name);

        foreach ((string key, object? value) in fields)
        {
            line.Append(' ')
                .Append(key)
                .Append('=')
                .Append(Escape(value));
        }

        string text = line.ToString();
        lock (Gate)
        {
            File.AppendAllText(TracePath, text + Environment.NewLine);
            File.AppendAllText("/tmp/skua-breadcrumb.log", text + Environment.NewLine);
        }
    }

    public static string Preview(string? value, int? maxLength = null)
    {
        if (value is null)
            return "<null>";

        int max = PayloadsEnabled ? 4096 : maxLength ?? DefaultPreviewLength;
        string normalized = value.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
        normalized = Redact(normalized);
        if (normalized.Length <= max)
            return normalized;
        return normalized[..max] + $"...(+{normalized.Length - max})";
    }

    public static string ArgSummary(object?[]? args)
    {
        if (args is null || args.Length == 0)
            return "argc=0";

        StringBuilder sb = new();
        sb.Append("argc=").Append(args.Length);
        int count = Math.Min(args.Length, 3);
        for (int i = 0; i < count; i++)
        {
            object? arg = args[i];
            sb.Append(" arg").Append(i).Append('=');
            sb.Append(arg switch
            {
                null => "<null>",
                string s => Preview(s),
                Array a => $"array[{a.Length}]",
                _ => Preview(arg.ToString())
            });
        }
        return sb.ToString();
    }

    public static string XmlSummary(string? xml)
    {
        if (xml is null)
            return "xml=<null>";
        return $"xmlLen={xml.Length} xml={Preview(xml)}";
    }

    private static bool ShouldWrite(string component, string name)
    {
        if (Enabled)
            return true;

        if (!IsImportant(component, name))
            return false;

        string key = component + ':' + name;
        int count = QuietEventCounts.AddOrUpdate(key, 1, (_, existing) => existing + 1);
        return count <= 20 || count % 100 == 0;
    }

    private static bool IsImportant(string component, string name)
    {
        string text = component + ':' + name;
        return text.Contains("error", StringComparison.OrdinalIgnoreCase)
            || text.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || text.Contains("exception", StringComparison.OrdinalIgnoreCase)
            || text.Contains("failed", StringComparison.OrdinalIgnoreCase)
            || text.Contains("crash", StringComparison.OrdinalIgnoreCase)
            || text.Contains("exited", StringComparison.OrdinalIgnoreCase)
            || text.Contains("close-frame", StringComparison.OrdinalIgnoreCase)
            || text.Contains("receive-loop-end", StringComparison.OrdinalIgnoreCase)
            || text.Contains("pending-failed", StringComparison.OrdinalIgnoreCase);
    }

    private static string Escape(object? value)
    {
        if (value is null)
            return "<null>";

        string text = value switch
        {
            string s => s,
            _ => value.ToString() ?? string.Empty
        };

        text = text.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
        if (text.Contains(' ') || text.Contains('\t') || text.Contains('=') || text.Contains('|'))
            return '"' + text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + '"';
        return text;
    }

    private static string Redact(string text)
    {
        return text
            .Replace("strPassword", "strPassword(redacted)", StringComparison.OrdinalIgnoreCase)
            .Replace("password", "password(redacted)", StringComparison.OrdinalIgnoreCase)
            .Replace("pwd", "pwd(redacted)", StringComparison.OrdinalIgnoreCase);
    }
}
