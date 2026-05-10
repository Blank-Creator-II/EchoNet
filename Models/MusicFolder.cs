namespace EchoNet.Models;

public class MusicFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Path { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime LastScannedAt { get; set; }
}