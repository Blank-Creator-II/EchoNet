using ATL;
using EchoNet.Models;
using EchoNet.ViewModels;

namespace EchoNet.Utils;

public static class MetadataHelper
{
    public static Song CreateSongFromFilePath(string filePath, Guid musicFolderId)
    {
        // Hit disk once using ATL to extract structural tags
        var track = new Track(filePath);
        var info = new FileInfo(filePath);

        return new Song
        {
            MusicFolderId = musicFolderId,
            FilePath = Path.GetFullPath(filePath),
            FileName = info.Name,
            Title = string.IsNullOrWhiteSpace(track.Title)
                ? Path.GetFileNameWithoutExtension(info.Name)
                : track.Title,
            Artist = string.IsNullOrWhiteSpace(track.Artist) ? "Unknown Artist" : track.Artist,
            Album = string.IsNullOrWhiteSpace(track.Album) ? "Unknown Album" : track.Album,
            Genre = string.IsNullOrWhiteSpace(track.Genre) ? "Unknown Genre" : track.Genre,
            FileSize = info.Length,
            LastModifiedUtc = info.LastWriteTimeUtc,
            Duration = TimeSpan.FromSeconds(track.Duration),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static SongMetadata ReadSongMetadata(Song song)
    {
        // Map values directly from  pre-existing database entity parameters
        // to avoid costly, repetitive physical disk reads on large collections.
        var cover = GetCoverArt(song.FilePath);

        return new SongMetadata
        {
            Id = song.Id,
            FilePath = song.FilePath,
            FileName = song.FileName,
            Title = song.Title,
            Artist = song.Artist,
            Album = song.Album,
            Genre = song.Genre,
            FileSize = song.FileSize,
            LastModifiedUtc = song.LastModifiedUtc,
            Duration = song.Duration,
            FormattedDuration = song.Duration.ToString(@"m\:ss"),
            CreatedAt = song.CreatedAt.ToString("o"),
            CoverArtBytes = cover?.Bytes,
            CoverArtContentType = cover?.ContentType
        };
    }

    public static CoverArtResult? GetCoverArt(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return null;

            var track = new Track(filePath);
            var picture = track.EmbeddedPictures.FirstOrDefault();

            if (picture is null || picture.PictureData is null || picture.PictureData.Length == 0)
            {
                return null;
            }

            return new CoverArtResult(
                Bytes: picture.PictureData,
                ContentType: GuessImageContentType(picture.PictureData));
        }
        catch
        {
            // Fail silently on disk access/corruption errors to ensure queue mapping stays alive
            return null;
        }
    }

    private static string GuessImageContentType(byte[] imageBytes)
    {
        if (imageBytes.Length >= 3 &&
            imageBytes[0] == 0xFF &&
            imageBytes[1] == 0xD8 &&
            imageBytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (imageBytes.Length >= 8 &&
            imageBytes[0] == 0x89 &&
            imageBytes[1] == 0x50 &&
            imageBytes[2] == 0x4E &&
            imageBytes[3] == 0x47)
        {
            return "image/png";
        }

        if (imageBytes.Length >= 6 &&
            imageBytes[0] == 0x47 &&
            imageBytes[1] == 0x49 &&
            imageBytes[2] == 0x46)
        {
            return "image/gif";
        }

        return "image/jpeg";
    }

    public sealed record CoverArtResult(byte[] Bytes, string ContentType);
}