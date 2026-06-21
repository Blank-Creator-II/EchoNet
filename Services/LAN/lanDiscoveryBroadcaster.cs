using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EchoNet.Models;

namespace EchoNet.Services;

public class LanDiscoveryBroadcaster : BackgroundService
{
    private readonly ILogger<LanDiscoveryBroadcaster> _logger;
    private readonly int _broadcastPort = 18345; // The port listeners are camping on
    private readonly int _webAppPort = 9292;     // app's HTTP port

    public LanDiscoveryBroadcaster(ILogger<LanDiscoveryBroadcaster> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LAN Discovery Broadcaster Service is starting.");

        using var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        var endPoint = new IPEndPoint(IPAddress.Broadcast, _broadcastPort);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                string? localIp = GetLocalIPAddress();
                
                if (!string.IsNullOrEmpty(localIp))
                {
                    // Construct the payload
                    var payload = new DiscoveryPayload
                    {
                        DeviceName = Environment.MachineName,
                        ApiBaseUrl = $"http://{localIp}:{_webAppPort}/api/music"
                    };

                    string jsonPayload = JsonSerializer.Serialize(payload);
                    byte[] data = Encoding.UTF8.GetBytes(jsonPayload);

                    await udpClient.SendAsync(data, data.Length, endPoint);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while broadcasting LAN discovery data.");
            }

            // Broadcast every 3 seconds
            await Task.Delay(3000, stoppingToken);
        }

        _logger.LogInformation("LAN Discovery Broadcaster Service is stopping.");
    }

    // Helper to get the actual local network IP (skipping loopbacks and virtual adapters)
    private string? GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
            {
                // Simple heuristic to avoid common virtual adapters if any exist
                if (!ip.ToString().StartsWith("169.254")) 
                {
                    return ip.ToString();
                }
            }
        }
        return null;
    }
}