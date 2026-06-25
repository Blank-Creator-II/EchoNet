using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using EchoNet.Models;
using EchoNet.Hubs;

namespace EchoNet.Services;

public class DiscoveredDeviceRegistry
{
    // Holds discovered servers.
    private readonly ConcurrentDictionary<string, (DiscoveryPayload Payload, DateTime LastSeen)> _devices = new();
    private readonly IHubContext<AudioHub> _hubContext;
    private readonly ILogger<DiscoveredDeviceRegistry> _logger;

    public DiscoveredDeviceRegistry(IHubContext<AudioHub> hubContext, ILogger<DiscoveredDeviceRegistry> logger)
    {
        _hubContext = hubContext;
        _logger = logger;

        // Start a background task to sweep expired devices every few seconds
        Task.Run(StartEvictionTimer);
    }

    public void UpdateDevice(DiscoveryPayload payload)
    {
        // Add or update the device and timestamp it
        _devices[payload.Ip] = (payload, DateTime.UtcNow);

        // Broadcast updated device list to frontend
        BroadcastUpdatedDevice();
    }

    private async Task StartEvictionTimer()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        while (await timer.WaitForNextTickAsync())
        {
            var expiryTime = DateTime.UtcNow.AddSeconds(-10);
            bool changed = false;

            foreach (var kvp in _devices)
            {
                if (kvp.Value.LastSeen < expiryTime)
                {
                    if (_devices.TryRemove(kvp.Key, out _))
                    {
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                BroadcastUpdatedDevice();
            }
        }
    }

    private async void BroadcastUpdatedDevice()
    {
        try
        {
            // Just return the active values directly now since it removes expired ones auto
            var activeDevices = _devices.Values.Select(x => x.Payload).ToList();
            await _hubContext.Clients.All.SendAsync("ReceiveLanDevice", activeDevices);            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send LAN device(s) via SignalR.");
        }
    }
}