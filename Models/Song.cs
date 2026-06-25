using System.ComponentModel.DataAnnotations.Schema;

namespace EchoNet.Models;

public class Song
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MusicFolderId { get; set; }
    public MusicFolder? MusicFolder { get; set; }

    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    public string Title { get; set; } = "Unknown Title";
    public string Artist { get; set; } = "Unknown Artist";
    public string Album { get; set; } = "Unknown Album";
    public string Genre { get; set; } = "Unknown Genre";

    public long FileSize { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    public TimeSpan Duration { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool HasCoverArt { get; set; } = false;

    [NotMapped]
    public string CoverArtDirectory => $"/data/cover/{Id}";
    [NotMapped]
    public string FormattedDuration => Duration.ToString(@"m\:ss");
    [NotMapped]
    public string FormattedCreatedAt => CreatedAt.ToString("o");
    [NotMapped]
    public bool IsLocal => FilePath.Contains("http://") ? false : true;  
}