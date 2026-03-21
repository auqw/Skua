namespace Skua.App.Avalonia.ViewModels.Packets;

public record InterceptedPacketViewModel(string Packet, bool? Outbound)
{
    public override string ToString()
    {
        return Packet;
    }
}