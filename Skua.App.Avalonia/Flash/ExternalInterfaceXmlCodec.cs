using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace Skua.App.Avalonia.Flash;

public static class ExternalInterfaceXmlCodec
{
    public static string EncodeInvoke(string function, params object?[] args)
    {
        StringBuilder req = new StringBuilder().Append($"<invoke name=\"{SecurityElement.Escape(function)}\" returntype=\"xml\">");
        if (args.Length > 0)
        {
            req.Append("<arguments>");
            foreach (object? arg in args)
                req.Append(EncodeValue(arg));
            req.Append("</arguments>");
        }
        req.Append("</invoke>");
        return req.ToString();
    }

    public static FlashCallback DecodeCallback(string xml)
    {
        XElement root = XElement.Parse(xml);
        string function = root.Attribute("name")?.Value ?? string.Empty;
        object?[] args = root.Element("arguments")?.Elements().Select(DecodeElement).ToArray() ?? [];
        return new FlashCallback(function, args);
    }

    public static object? DecodeReturn(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        XElement root = XElement.Parse(xml);
        if (root.Name == "invoke")
            return DecodeCallback(xml);

        return DecodeElement(root);
    }

    private static string EncodeValue(object? value)
    {
        return value switch
        {
            null => "<null />",
            bool b => b ? "<true />" : "<false />",
            byte or short or int or long or float or double or decimal => $"<number>{Convert.ToString(value, CultureInfo.InvariantCulture)}</number>",
            Array array => EncodeArray(array),
            IDictionary<string, object?> dictionary => EncodeObject(dictionary),
            IReadOnlyDictionary<string, object?> dictionary => EncodeObject(dictionary),
            _ => $"<string>{SecurityElement.Escape(value.ToString())}</string>"
        };
    }

    private static string EncodeArray(Array array)
    {
        StringBuilder sb = new("<array>");
        int i = 0;
        foreach (object? item in array)
            sb.Append($"<property id=\"{i++}\">{EncodeValue(item)}</property>");
        return sb.Append("</array>").ToString();
    }

    private static string EncodeObject(IEnumerable<KeyValuePair<string, object?>> properties)
    {
        StringBuilder sb = new("<object>");
        foreach ((string key, object? item) in properties)
            sb.Append($"<property id=\"{SecurityElement.Escape(key)}\">{EncodeValue(item)}</property>");
        return sb.Append("</object>").ToString();
    }

    private static object? DecodeElement(XElement el)
    {
        return el.Name.LocalName switch
        {
            "string" => el.Value,
            "number" => double.TryParse(el.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) ? d : 0d,
            "true" => true,
            "false" => false,
            "null" => null,
            "array" => DecodeArray(el),
            "object" => DecodeObject(el),
            "property" => DecodeElement(el.Elements().First()),
            _ => el.Value
        };
    }

    private static object?[] DecodeArray(XElement el)
    {
        return el.Elements("property")
            .Select(p => new
            {
                Index = int.TryParse(p.Attribute("id")?.Value, out int i) ? i : 0,
                Value = DecodeElement(p.Elements().First())
            })
            .OrderBy(p => p.Index)
            .Select(p => p.Value)
            .ToArray();
    }

    private static Dictionary<string, object?> DecodeObject(XElement el)
    {
        return el.Elements("property")
            .ToDictionary(p => p.Attribute("id")?.Value ?? string.Empty, p => DecodeElement(p.Elements().First()));
    }
}

public sealed record FlashCallback(string Function, object?[] Args);
