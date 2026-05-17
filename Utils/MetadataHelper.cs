using ATL;
using EchoNet.Models;
using EchoNet.ViewModels;

namespace EchoNet.Utils;

public static class MetadataHelper
{
    public static Song CreateSongFromFilePath(string filePath, Guid musicFolderId)
    {
        var track = new Track(filePath);
        var info = new FileInfo(filePath);

        var cover = GetCoverArt(filePath);

        return new Song
        {
            MusicFolderId = musicFolderId,
            FilePath = Path.GetFullPath(filePath),
            FileName = info.Name,
            Title = string.IsNullOrWhiteSpace(track.Title)
                ? Path.GetFileNameWithoutExtension(info.Name)
                : track.Title,
            Artist = string.IsNullOrWhiteSpace(track.Artist) ? null : track.Artist,
            Album = string.IsNullOrWhiteSpace(track.Album) ? null : track.Album,
            Genre = string.IsNullOrWhiteSpace(track.Genre) ? null : track.Genre,
            FileSize = info.Length,
            LastModifiedUtc = info.LastWriteTimeUtc,
            Duration = TimeSpan.FromSeconds(track.Duration),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static TrackMetadataResult ReadTrackMetadata(string filePath)
    {
        var track = new Track(filePath);
        var info = new FileInfo(filePath);
        var cover = GetCoverArt(filePath);

        return new TrackMetadataResult
        {
            FilePath = Path.GetFullPath(filePath),
            FileName = info.Name,
            Title = string.IsNullOrWhiteSpace(track.Title)
                ? Path.GetFileNameWithoutExtension(info.Name)
                : track.Title,
            Artist = string.IsNullOrWhiteSpace(track.Artist) ? null : track.Artist,
            Album = string.IsNullOrWhiteSpace(track.Album) ? null : track.Album,
            Genre = string.IsNullOrWhiteSpace(track.Genre) ? null : track.Genre,
            FileSize = info.Length,
            LastModifiedUtc = info.LastWriteTimeUtc,
            Duration = TimeSpan.FromSeconds(track.Duration),
            CoverArtBytes = cover?.Bytes,
            CoverArtContentType = cover?.ContentType
        };
    }

    public static CoverArtResult? GetCoverArt(string filePath)
    {
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