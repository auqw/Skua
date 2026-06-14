using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace Skua.App.Avalonia.Flash;

public static class FlashXmlCodec
{
    public static string BuildInvokeXml(string function, params object?[] args)
    {
        StringBuilder req = new StringBuilder().Append($"<invoke name=\"{SecurityElement.Escape(function)}\" returntype=\"xml\">");
        if (args.Length > 0)
        {
            req.Append("<arguments>");
            foreach (object? arg in args)
                req.Append(ToFlashXml(arg));
            req.Append("</arguments>");
        }
        req.Append("</invoke>");
        return req.ToString();
    }

    public static string ToFlashXml(object? o)
    {
        switch (o)
        {
            case null:
                return "<null/>";

            case bool b:
                return $"<{b.ToString().ToLowerInvariant()}/>";

            case double:
            case float:
            case long:
            case int:
            case short:
            case byte:
            case decimal:
                return $"<number>{o}</number>";

            case ExpandoObject:
                StringBuilder sb = new StringBuilder().Append("<object>");
                foreach (KeyValuePair<string, object?> kvp in (o as IDictionary<string, object?>)!)
                    sb.Append($"<property id=\"{SecurityElement.Escape(kvp.Key)}\">{ToFlashXml(kvp.Value)}</property>");
                return sb.Append("</object>").ToString();

            default:
                if (o is Array array)
                {
                    StringBuilder arrayBuilder = new StringBuilder().Append("<array>");
                    int k = 0;
                    foreach (object? el in array)
                        arrayBuilder.Append($"<property id=\"{k++}\">{ToFlashXml(el)}</property>");
                    return arrayBuilder.Append("</array>").ToString();
                }
                return $"<string>{SecurityElement.Escape(o.ToString())}</string>";
        }
    }

    public static object? FromFlashXml(XElement el)
    {
        switch (el.Name.ToString())
        {
            case "number":
                return int.TryParse(el.Value, out int i) ? i : float.TryParse(el.Value, out float f) ? f : 0;

            case "true":
                return true;

            case "false":
                return false;

            case "null":
                return null;

            case "array":
                return el.Elements().Select(e => FromFlashXml(e.Elements().FirstOrDefault() ?? e)).ToArray();

            case "object":
                IDictionary<string, object?> d = new ExpandoObject();
                foreach (XElement e in el.Elements())
                {
                    string key = e.Attribute("id")!.Value;
                    XElement value = e.Elements().FirstOrDefault() ?? e;
                    d[key] = FromFlashXml(value);
                }
                return (ExpandoObject)d;

            default:
                return el.Value;
        }
    }
}
