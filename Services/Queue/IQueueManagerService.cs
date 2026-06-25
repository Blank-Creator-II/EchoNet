using EchoNet.Models;
using EchoNet.ViewModels;
using Microsoft.AspNetCore.SignalR;

namespace EchoNet.Services;

public interface IQueueManagerService
{
    public PlayerState GetPlayerState();
    public void SetPlayerState(PlayerState _playerState);
    public Song? FindSongByIdFromQueue(Guid id, QueueType type);
    public Task<bool> GenerateQueue(QueueType type, List<Song>? songs = null);
    public void SortQueue(QueueType type, QueueState orderMethod, int? seed = null);
    public List<Song> GetQueue(QueueType type);
    public void OnSongEnd(object? sender, EventArgs e);
    public void OnSongError(object? sender, EventArgs e);
    public Task PlayNextAsync();
    public Task PlayPreviousAsync();
}