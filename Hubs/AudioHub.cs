using Microsoft.AspNetCore.SignalR;

namespace EchoNet.Hubs;

public class AudioHub : Hub
{
    // we don't necessarily need methods here yet, because the Server (VLC Service) 
    // is what will be pushing updates DOWN to the clients.
}