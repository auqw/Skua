using Skua.App.Avalonia.Flash;
using System.Dynamic;
using Xunit;

namespace Skua.App.Avalonia.Tests;

public sealed class ExternalInterfaceXmlCodecTests
{
    [Fact]
    public void EncodeInvoke_WritesExternalInterfaceXml()
    {
        string xml = ExternalInterfaceXmlCodec.EncodeInvoke("world.gotoTown", "battleon", 1, true, null);

        Assert.Equal("<invoke name=\"world.gotoTown\" returntype=\"xml\"><arguments><string>battleon</string><number>1</number><true /><null /></arguments></invoke>", xml);
    }

    [Fact]
    public void DecodeCallback_ReadsFunctionAndArguments()
    {
        var callback = ExternalInterfaceXmlCodec.DecodeCallback("<invoke name=\"packet\" returntype=\"xml\"><arguments><string>%xt%</string><number>5</number><false /></arguments></invoke>");

        Assert.Equal("packet", callback.Function);
        Assert.Equal(["%xt%", 5d, false], callback.Args);
    }

    [Fact]
    public void DecodeReturn_ReadsPrimitiveXml()
    {
        Assert.Equal("ok", ExternalInterfaceXmlCodec.DecodeReturn("<string>ok</string>"));
        Assert.Equal(7d, ExternalInterfaceXmlCodec.DecodeReturn("<number>7</number>"));
        Assert.Equal(true, ExternalInterfaceXmlCodec.DecodeReturn("<true />"));
        Assert.Null(ExternalInterfaceXmlCodec.DecodeReturn("<null />"));
    }

    [Fact]
    public void DecodeCallback_ReadsArrayArguments()
    {
        var callback = ExternalInterfaceXmlCodec.DecodeCallback("<invoke name=\"loaded\"><arguments><array><property id=\"1\"><string>b</string></property><property id=\"0\"><string>a</string></property></array></arguments></invoke>");

        object?[] array = Assert.IsType<object?[]>(callback.Args[0]);
        Assert.Equal(["a", "b"], array);
    }

    [Fact]
    public void EncodeInvoke_WritesExpandoObject()
    {
        dynamic expando = new ExpandoObject();
        expando.ItemID = 123;
        expando.Name = "Sword";

        string xml = ExternalInterfaceXmlCodec.EncodeInvoke("useItem", expando);

        Assert.Contains("<object>", xml);
        Assert.Contains("<property id=\"ItemID\"><number>123</number></property>", xml);
        Assert.Contains("<property id=\"Name\"><string>Sword</string></property>", xml);
    }

    [Fact]
    public void DecodeCallback_ReadsObjectArguments()
    {
        var callback = ExternalInterfaceXmlCodec.DecodeCallback("<invoke name=\"obj\"><arguments><object><property id=\"ItemID\"><number>123</number></property><property id=\"Name\"><string>Sword</string></property></object></arguments></invoke>");

        var obj = Assert.IsType<Dictionary<string, object?>>(callback.Args[0]);
        Assert.Equal(123d, obj["ItemID"]);
        Assert.Equal("Sword", obj["Name"]);
    }
}
