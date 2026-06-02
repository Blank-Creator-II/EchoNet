using EchoNet.Models;
using EchoNet.ViewModels;
using Microsoft.AspNetCore.SignalR;

namespace EchoNet.Services;

public interface IQueueManagerService
{
    public PlayerState GetPlayerState();
    public void SetPlayerState(PlayerState _playerState);
    public Task<bool> GenerateQueue();
    public void SortQueue(QueueState orderMethod, int? seed = null);
    public List<SongMetadata> GetQueue();
    public void OnSongEnd(object? sender, EventArgs e);
    public Task PlayNextAsync();
    public Task PlayPreviousAsync();
}