using EchoNet.Data;
using EchoNet.Models;
using EchoNet.Utils;
using Microsoft.EntityFrameworkCore;

namespace EchoNet.Services;

public class LibScannerService : ILibScannerService
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".flac", ".aac", ".m4a", ".ogg"
        };

    private readonly AppDbContext _db;
    private readonly ISongService _songService;
    private readonly ILogger<LibScannerService> _logger;

    public LibScannerService(
        AppDbContext db,
        ISongService songService,
        ILogger<LibScannerService> logger)
    {
        _db = db;
        _songService = songService;
        _logger = logger;
    }

    public async Task<bool> NeedsFirstTimeSetupAsync(CancellationToken cancellationToken = default)
    {
        var hasFolders = await _db.MusicFolders.AnyAsync(cancellationToken);
        return !hasFolders;
    }

    public async Task<bool> RunFirstTimeSetupAsync(IReadOnlyList<string> selectedFolders, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting first-time library setup.");

        var needsSetup = await NeedsFirstTimeSetupAsync(cancellationToken);
        if (!needsSetup)
        {
            _logger.LogInformation("Library setup already exists. Skipping first-time setup.");
            return true;
        }

        if (selectedFolders.Count == 0)
        {
            _logger.LogWarning("No folders were selected. First-time setup aborted.");
            return false;
        }

        var musicFolders = new List<MusicFolder>();

        foreach (var folderPath in selectedFolders)
        {
            var fullPath = NormalizePath(folderPath);

            if (!Directory.Exists(fullPath))
            {
                _logger.LogWarning("Selected folder does not exist: {FolderPath}", fullPath);
                continue;
            }

            var folder = new MusicFolder
            {
                Id = Guid.NewGuid(),
                Path = fullPath,
                IsEnabled = true,
                LastScannedAt = DateTime.UtcNow
            };

            musicFolders.Add(folder);
            _logger.LogInformation("Accepted music folder: {FolderPath}", fullPath);
        }

        if (musicFolders.Count == 0)
        {
            _logger.LogWarning("No valid folders remained after validation.");
            return false;
        }

        _db.MusicFolders.AddRange(musicFolders);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved {FolderCount} music folder(s). Starting initial scan.", musicFolders.Count);

        var songs = new List<Song>();

        foreach (var folder in musicFolders)
        {
            foreach (var filePath in Directory.EnumerateFiles(folder.Path, "*.*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var extension = Path.GetExtension(filePath);
                if (!SupportedExtensions.Contains(extension))
                    continue;

                try
                {
                    var song = MetadataHelper.CreateSongFromFilePath(filePath, folder.Id);
                    songs.Add(song);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read metadata for file: {FilePath}", filePath);
                }
            }
        }

        _logger.LogInformation("Initial scan found {SongCount} song(s). Saving to database.", songs.Count);

        if (songs.Count > 0)
        {
            var success = await _songService.AddSongsAsync(songs);
            if (!success)
            {
                _logger.LogError("Initial song save failed.");
                return false;
            }
        }

        _logger.LogInformation("First-time library setup complete.");
        return true;
    }

    private static string NormalizePath(string path)
    {
        var full = Path.GetFullPath(path);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}