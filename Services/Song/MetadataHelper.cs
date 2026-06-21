using ATL;
using SkiaSharp;
using EchoNet.Models;
using ATL.Logging;

namespace EchoNet.Services;

public class MetadataHelper
{
    private readonly string CoverArtDirectory;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MetadataHelper> _logger;
    private static readonly int[] TargetResolutions = [64, 256, 512];

    public MetadataHelper(IWebHostEnvironment env, ILogger<MetadataHelper> logger)
    {
        _env = env;
        _logger = logger;
        CoverArtDirectory = Path.Combine(env.WebRootPath, "data", "cover");
    }

    public Song CreateSongFromFilePath(string filePath, Guid musicFolderId)
    {
        // Hit disk once using ATL to extract structural tags
        var track = new Track(filePath);
        var info = new FileInfo(filePath);

        return new Song
        {
            MusicFolderId = musicFolderId,
            FilePath = Path.GetFullPath(filePath),
            FileName = info.Name,
            Title = string.IsNullOrWhiteSpace(track.Title) ? Path.GetFileNameWithoutExtension(info.Name) : track.Title,
            Artist = string.IsNullOrWhiteSpace(track.Artist) ? "Unknown Artist" : track.Artist,
            Album = string.IsNullOrWhiteSpace(track.Album) ? "Unknown Album" : track.Album,
            Genre = string.IsNullOrWhiteSpace(track.Genre) ? "Unknown Genre" : track.Genre,
            FileSize = info.Length,
            LastModifiedUtc = info.LastWriteTimeUtc,
            Duration = TimeSpan.FromSeconds(track.Duration),
            CreatedAt = DateTime.UtcNow,
            HasCoverArt = track.EmbeddedPictures.Any()
        };
    }

    public void SaveCoverArt(Song song)
    {
        try
        {
            if (!File.Exists(song.FilePath)) return;
            
            var track = new Track(song.FilePath);
            var picture = track.EmbeddedPictures.FirstOrDefault();

            if (picture?.PictureData == null || picture.PictureData.Length == 0)
            {
                return;
            }

            if (!Directory.Exists(CoverArtDirectory))
            {
                Directory.CreateDirectory(CoverArtDirectory);
            }

            // Decode raw embedded byte array into a Skia Bitmap
            using var originalBitmap = SKBitmap.Decode(picture.PictureData);
            if (originalBitmap == null) return; // Handle corrupt image streams safely

            foreach (var size in TargetResolutions)
            {
                // Calculate aspect-ratio safe constraints
                float ratio = Math.Min((float)size / originalBitmap.Width, (float)size / originalBitmap.Height);
                int newWidth = Math.Max(1, (int)(originalBitmap.Width * ratio));
                int newHeight = Math.Max(1, (int)(originalBitmap.Height * ratio));

                // Perform downsampling
                using var resizedBitmap = originalBitmap.Resize(
                    new SKImageInfo(newWidth, newHeight), 
                    new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear)
                );

                if (resizedBitmap == null) continue;

                string fileName = $"{song.Id}_{size}.jpg";
                string fullOutputPath = Path.Combine(CoverArtDirectory, fileName);

                // Convert bitmap to an encoded image and dump to disk as a JPEG
                using var image = SKImage.FromBitmap(resizedBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 100);
                using var stream = File.OpenWrite(fullOutputPath);
                
                data.SaveTo(stream);
            }

            _logger.LogDebug("Generated coverArt for ID: {SongID}.", song.Id);
        }
        catch (Exception ex)
        {
            // Fail silently to keep processing loop alive
            _logger.LogError(ex,"Failed to generate coverArt for ID: {SongID}.", song.Id);
        }   
    }
}