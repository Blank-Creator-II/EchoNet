using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EchoNet.Models;
using EchoNet.Utils;

namespace EchoNet.Services;

public class LanDiscoveryBroadcaster : BackgroundService
{
    private readonly ILogger<LanDiscoveryBroadcaster> _logger;
    private readonly AppDataJsonReader _appDataReader;
    private readonly int _broadcastPort = 18345; // The port listeners are camping on

    public LanDiscoveryBroadcaster(ILogger<LanDiscoveryBroadcaster> logger, AppDataJsonReader appDataReader)
    {
        _logger = logger;
        _appDataReader = appDataReader;
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
                string? localIp = LANHelper.GetLocalIPAddress();
                
                if (!string.IsNullOrEmpty(localIp))
                {
                    // Construct the payload
                    var payload = new DiscoveryPayload
                    {
                        HostId = _appDataReader.Current.AppId,
                        DeviceName = Environment.MachineName,
                        Ip = localIp
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
}