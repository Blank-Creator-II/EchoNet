using EchoNet.Models;

namespace EchoNet.ViewModels;

public class PlayerViewModel
{
    public List<Song> Songs { get; set; } = new();
    public string? CurrentSongTitle { get; set; }
    public bool IsPlaying { get; set; }
}