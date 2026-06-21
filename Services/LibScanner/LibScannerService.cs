using System.Diagnostics;
using EchoNet.Data;
using EchoNet.Models;
using EchoNet.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
    private readonly MetadataHelper _metadataHelper;
    private readonly ILogger<LibScannerService> _logger;

    public LibScannerService(
        AppDbContext db,
        ISongService songService,
        ILogger<LibScannerService> logger,
        MetadataHelper metadataHelper)
    {
        _db = db;
        _songService = songService;
        _metadataHelper = metadataHelper;
        _logger = logger;
    }

    public async Task<bool> ScanFoldersAsync(IReadOnlyList<string> selectedFolders, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting library scan.");

        if (selectedFolders.Count == 0)
        {
            _logger.LogWarning("No folders were provided for scanning.");
            return false;
        }

        // Performance tracking
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var existingPaths = await _db.MusicFolders
                .Select(f => f.Path)
                .ToListAsync(cancellationToken);

            var existingFoldersSet = new HashSet<string>(existingPaths, StringComparer.OrdinalIgnoreCase);
            var musicFoldersToTrack = new List<MusicFolder>();

            foreach (var folderPath in selectedFolders)
            {
                var fullPath = NormalizePath(folderPath);

                if (existingFoldersSet.Contains(fullPath))
                {
                    _logger.LogDebug("Folder already exists in library, skipping: {FolderPath}", fullPath);
                    continue;
                }

                if (!Directory.Exists(fullPath))
                {
                    _logger.LogWarning("Provided folder does not exist: {FolderPath}", fullPath);
                    continue;
                }

                var folder = new MusicFolder
                {
                    Id = Guid.NewGuid(),
                    Path = fullPath,
                    IsEnabled = true,
                    LastScannedAt = DateTime.UtcNow
                };

                musicFoldersToTrack.Add(folder);
                _logger.LogDebug("Accepted new music folder for tracking: {FolderPath}", fullPath);
        }

            // If all input folders were duplicates or invalid, exit early
            if (musicFoldersToTrack.Count == 0)
            {
                _logger.LogInformation("No new folders to process.");
                return true; 
            }

            _db.MusicFolders.AddRange(musicFoldersToTrack);
            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Saved {FolderCount} new music folder(s). Beginning file discovery.", musicFoldersToTrack.Count);

            var songs = new List<Song>();

            foreach (var folder in musicFoldersToTrack)
            {
                try
                {
                    foreach (var filePath in Directory.EnumerateFiles(folder.Path, "*.*", SearchOption.AllDirectories))
                    {
                        // Safely exits iteration if requested by client lifecycle thread (not yet setup)
                        cancellationToken.ThrowIfCancellationRequested();

                        var extension = Path.GetExtension(filePath);
                        if (!SupportedExtensions.Contains(extension))
                            continue;

                        try
                        {
                            var song = _metadataHelper.CreateSongFromFilePath(filePath, folder.Id);
                            _metadataHelper.SaveCoverArt(song);
                            songs.Add(song);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to read metadata for file: {FilePath}", filePath);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("File parsing loop successfully stopped via user request.");
                    throw; 
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Access denined while checking path: {FolderPath}", folder.Path);
                }
            }

            _logger.LogInformation("Scan found {SongCount} new song(s). Saving to database.", songs.Count);

            if (songs.Count > 0)
            {
                var success = await _songService.AddSongsAsync(songs);
                if (!success)
                {
                    _logger.LogError("Failed to save newly discovered songs.");
                    return false;
                }
            }

            stopwatch.Stop();
            _logger.LogInformation("Library scan completed. Processing Duration: {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Library file scanning was terminated by cancle request.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled general system exception crashed library scanner.");
            return false;
        }
    }

    private static string NormalizePath(string path)
    {
        var full = Path.GetFullPath(path);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}