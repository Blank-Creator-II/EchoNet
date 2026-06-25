using EchoNet.Services;

namespace EchoNet.Models;

public class DiscoveryPayload
{
    public Guid HostId { get; set; } 
    public string DeviceName { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
}