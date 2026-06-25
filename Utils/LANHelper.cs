using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using EchoNet.Models;
using EchoNet.ViewModels;

namespace EchoNet.Utils;

public static class LANHelper
{
    public static string? GetLocalIPAddress()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                // Force the OS to evaluate the routing path for this destination
                socket.Connect("8.8.8.8", 65530);
                
                if (socket.LocalEndPoint is IPEndPoint localEndPoint)
                {
                    return localEndPoint.Address.ToString();
                }
            }
        }
        catch (Exception)
        {
            return GetBackupLocalIPAddress();
        }

        return null;
    }

    private static string? GetBackupLocalIPAddress()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var ni in interfaces)
            {
                // Ensure the interface is up and not a loopback
                if (ni.OperationalStatus != OperationalStatus.Up || 
                    ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                // Skip common virtual/software adapters
                string desc = ni.Description.ToLower();
                string name = ni.Name.ToLower();
                if (desc.Contains("virtual") || desc.Contains("pseudo") || desc.Contains("docker") || desc.Contains("wsl") ||
                    name.Contains("vEthernet") || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                {
                    continue;
                }

                var ipProperties = ni.GetIPProperties();
                foreach (var unicast in ipProperties.UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ipStr = unicast.Address.ToString();

                        // Skip APIPA (Link-Local)
                        if (!ipStr.StartsWith("169.254"))
                        {
                            return ipStr;
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            // pass
        }

        return "127.0.0.1"; 
    }

    public static Song ToSong(this SongDTO songDTO)
    {
        if (songDTO == null) {throw new ArgumentNullException(nameof(songDTO));}
        
        return new Song
        {
            Id = songDTO.Id,
            FilePath = songDTO.HostUrl,
            Title = songDTO.Title,
            Artist = songDTO.Artist,
            Album = songDTO.Album,
            Duration = songDTO.Duration,
            CreatedAt = songDTO.CreatedAt,
            HasCoverArt = songDTO.HasCoverArt
        };
    }
}