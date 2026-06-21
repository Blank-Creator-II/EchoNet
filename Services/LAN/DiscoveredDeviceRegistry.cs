using System.Collections.Concurrent;
using EchoNet.Models;

namespace EchoNet.Services;

public class DiscoveredDeviceRegistry
{
    // Holds discovered servers.
    private readonly ConcurrentDictionary<string, (DiscoveryPayload Payload, DateTime LastSeen)> _devices = new();

    public void UpdateDevice(DiscoveryPayload payload)
    {
        // Add or update the device and timestamp it
        _devices[payload.ApiBaseUrl] = (payload, DateTime.UtcNow);
    }

    public List<DiscoveryPayload> GetActiveDevices()
    {
        // Remove devices that haven't broadcasted in the last 10 seconds
        var expiryTime = DateTime.UtcNow.AddSeconds(-10);
        foreach (var key in _devices.Keys)
        {
            if (_devices[key].LastSeen < expiryTime)
            {
                _devices.TryRemove(key, out _);
            }
        }

        return _devices.Values.Select(x => x.Payload).ToList();
    }
}