using ATL;
using EchoNet.Models;
using EchoNet.ViewModels;

namespace EchoNet.Utils;

public static class MetadataHelper
{
    public static Song CreateSongFromFilePath(string filePath, Guid musicFolderId)
    {
        var song = new Track(filePath);
        var info = new FileInfo(filePath);

        var cover = GetCoverArt(filePath);

        return new Song
        {
            MusicFolderId = musicFolderId,
            FilePath = Path.GetFullPath(filePath),
            FileName = info.Name,
            Title = string.IsNullOrWhiteSpace(song.Title)
                ? Path.GetFileNameWithoutExtension(info.Name)
                : song.Title,
            Artist = string.IsNullOrWhiteSpace(song.Artist) ? null : song.Artist,
            Album = string.IsNullOrWhiteSpace(song.Album) ? null : song.Album,
            Genre = string.IsNullOrWhiteSpace(song.Genre) ? null : song.Genre,
            FileSize = info.Length,
            LastModifiedUtc = info.LastWriteTimeUtc,
            Duration = TimeSpan.FromSeconds(song.Duration),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static SongMetadata ReadSongMetadata(Song _song)
    {
        var song = new Track(_song.FilePath);
        var info = new FileInfo(_song.FilePath);
        var cover = GetCoverArt(_song.FilePath);

        return new SongMetadata
        {
            Id = _song.Id,
            FilePath = Path.GetFullPath(_song.FilePath),
            FileName = info.Name,
            Title = string.IsNullOrWhiteSpace(song.Title)
                ? Path.GetFileNameWithoutExtension(info.Name)
                : song.Title,
            Artist = string.IsNullOrWhiteSpace(song.Artist) ? "Unknown Artist" : song.Artist,
            Album = string.IsNullOrWhiteSpace(song.Album) ? "Unknown Album" : song.Album,
            Genre = string.IsNullOrWhiteSpace(song.Genre) ? "Unknown Genre" : song.Genre,
            FileSize = info.Length,
            LastModifiedUtc = info.LastWriteTimeUtc,
            Duration = TimeSpan.FromSeconds(song.Duration),
            FormattedDuration = TimeSpan.FromSeconds(song.Duration).ToString(@"m\:ss"),   // format "3:20"
            CreatedAt = _song.CreatedAt.ToString("o"), // formated to be specfic to help the sorter
            CoverArtBytes = cover?.Bytes,
            CoverArtContentType = cover?.ContentType
        };
    }

    public static CoverArtResult? GetCoverArt(string filePath)
    {
        var song = new Track(filePath);
        var picture = song.EmbeddedPictures.FirstOrDefault();

        if (picture is null || picture.PictureData is null || picture.PictureData.Length == 0)
        {
            return null;
        }

        return new CoverArtResult(
            Bytes: picture.PictureData,
            ContentType: GuessImageContentType(picture.PictureData));
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