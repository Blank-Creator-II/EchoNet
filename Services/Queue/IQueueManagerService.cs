using EchoNet.Models;
using EchoNet.ViewModels;
using Microsoft.AspNetCore.SignalR;

namespace EchoNet.Services;

public interface IQueueManagerService
{
    public PlayerState GetPlayerState();
    public void SetPlayerState(PlayerState _playerState);
    public Song? FindSongByIdFromQueue(Guid id);
    public Task<bool> GenerateQueue();
    public void SortQueue(QueueState orderMethod, int? seed = null);
    public List<Song> GetQueue();
    public void OnSongEnd(object? sender, EventArgs e);
    public Task PlayNextAsync();
    public Task PlayPreviousAsync();
}