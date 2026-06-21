using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using EchoNet.Models;

namespace EchoNet.Services;

public class LanDiscoveryListener : BackgroundService
{
    private readonly ILogger<LanDiscoveryListener> _logger;
    private readonly DiscoveredDeviceRegistry _discoveredDevice;
    private readonly int _listenPort = 18345;

    public LanDiscoveryListener(ILogger<LanDiscoveryListener> logger, DiscoveredDeviceRegistry discoveredDevice)
    {
        _logger = logger;
        _discoveredDevice = discoveredDevice;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Listening for EchoNet broadcasters on port {Port}", _listenPort);

        // Bind to IPAddress.Any to listen on all network interfaces
        using var udpClient = new UdpClient(_listenPort);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait asynchronously for a broadcast packet
                var result = await udpClient.ReceiveAsync(stoppingToken);
                string jsonString = Encoding.UTF8.GetString(result.Buffer);

                var payload = JsonSerializer.Deserialize<DiscoveryPayload>(jsonString);
                if (payload != null)
                {
                    _discoveredDevice.UpdateDevice(payload);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving LAN discovery packet.");
            }
        }
    }
}