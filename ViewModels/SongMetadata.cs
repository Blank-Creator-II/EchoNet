using EchoNet.Migrations;

namespace EchoNet.ViewModels;

public sealed class SongMetadata
{
    public Guid Id { get; init; }
    public string FilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;
    public string? Artist { get; init; }
    public string? Album { get; init; }
    public string? Genre { get; init; }

    public long FileSize { get; init; }
    public DateTime LastModifiedUtc { get; init; }

    public TimeSpan Duration { get; init; }
    public string? FormattedDuration { get; init; }
    public string? CreatedAt { get; init; }

    public byte[]? CoverArtBytes { get; init; }
    public string? CoverArtContentType { get; init; }

    public string? CoverArtDataUri =>
        CoverArtBytes is null || CoverArtBytes.Length == 0
            ? null
            : $"data:{CoverArtContentType};base64,{Convert.ToBase64String(CoverArtBytes)}";
}