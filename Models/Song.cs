namespace EchoNet.Models;

public class Song
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MusicFolderId { get; set; }
    public MusicFolder? MusicFolder { get; set; }

    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Genre { get; set; }

    public long FileSize { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}