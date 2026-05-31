using EchoNet.Data;
using EchoNet.Models;
using EchoNet.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace EchoNet.Services;

public class SongService : ISongService
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".flac", ".aac", ".m4a", ".ogg"
        };

    private readonly AppDbContext _db;
    private readonly ILogger<SongService> _logger;

    public SongService(AppDbContext db, ILogger<SongService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<bool> AddSongsAsync(List<Song> songs)
    {
        if (songs is null || songs.Count == 0)
        {
            _logger.LogWarning("AddSongsAsync was called with no songs.");
            return false;
        }

        try
        {
            _logger.LogInformation("Adding {Count} song(s) to the database.", songs.Count);

            _db.Songs.AddRange(songs);
            var saved = await _db.SaveChangesAsync();

            _logger.LogInformation("Successfully added {Count} song(s) to the database.", saved);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add songs to the database.");
            return false;
        }
    }

    public async Task<List<Song>> GetAllSongsAsync()
    {
        _logger.LogDebug("Fetching all songs from the database.");
        return await _db.Songs.ToListAsync();
    }

    public async Task<Song?> GetSongByIdAsync(Guid id)
    {
        _logger.LogDebug("Fetching song by ID: {SongId}", id);
        return await _db.Songs.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Song>> SearchByTitleAsync(string text)
    {
        _logger.LogDebug("Searching songs by title with text: {SearchText}", text);

        return await _db.Songs
            .Where(s => s.Title.Contains(text))
            .ToListAsync();
    }

    public async Task<List<Song>> SearchByGenreAsync(string genre)
    {
        _logger.LogDebug("Searching songs by genre: {Genre}", genre);

        return await _db.Songs
            .Where(s => s.Genre == genre)
            .ToListAsync();
    }

    public async Task<List<Song>> SearchByArtistAsync(string artist)
    {
        _logger.LogDebug("Searching songs by artist with text: {Artist}", artist);

        return await _db.Songs
            .Where(s => s.Artist != null && s.Artist.Contains(artist))
            .ToListAsync();
    }

    public async Task<Song?> GetSongByPathAsync(string path)
    {
        return await _db.Songs
            .FirstOrDefaultAsync(s => s.FilePath == path);
    }

    public async Task<List<Song>> GeneralSearchAsync(string term)
    {
        _logger.LogDebug("Performing general song search with term: {Term}", term);

        return await _db.Songs
            .Where(s =>
                s.Title.Contains(term) ||
                (s.Artist != null && s.Artist.Contains(term)) ||
                (s.Album != null && s.Album.Contains(term)) ||
                (s.Genre != null && s.Genre.Contains(term)))
            .ToListAsync();
    }

    public async Task<bool> DeleteSongAsync(Guid id)
    {
        _logger.LogInformation("Deleting song with ID: {SongId}", id);

        var song = await _db.Songs.FirstOrDefaultAsync(s => s.Id == id);
        if (song is null)
        {
            _logger.LogWarning("DeleteSongAsync failed. Song not found: {SongId}", id);
            return false;
        }

        try
        {
            _db.Songs.Remove(song);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Song deleted successfully: {SongId} - {Title}", song.Id, song.Title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete song: {SongId}", id);
            return false;
        }
    }

    public async Task<LibrarySyncResult> SyncLibraryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting library sync.");

        var folders = await _db.MusicFolders
            .Where(f => f.IsEnabled)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {FolderCount} enabled music folder(s).", folders.Count);

        if (folders.Count == 0)
        {
            _logger.LogWarning("No enabled music folders found. Sync aborted.");
            return new LibrarySyncResult(0, 0, 0, 0, 0, 0);
        }

        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        var normalizedFolders = new List<(MusicFolder Folder, string Path)>();

        foreach (var folder in folders)
        {
            var folderPath = NormalizePath(folder.Path);

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                _logger.LogWarning("Skipping empty folder path for folder ID {FolderId}.", folder.Id);
                continue;
            }

            if (!Directory.Exists(folderPath))
            {
                _logger.LogWarning("Music folder does not exist and will be skipped: {FolderPath}", folderPath);
                continue;
            }

            _logger.LogInformation("Music folder accepted: {FolderPath}", folderPath);
            normalizedFolders.Add((folder, folderPath));
        }

        if (normalizedFolders.Count == 0)
        {
            _logger.LogWarning("No valid music folders found. Sync aborted.");
            return new LibrarySyncResult(0, 0, 0, 0, 0, folders.Count);
        }

        var folderIds = normalizedFolders.Select(x => x.Folder.Id).ToList();

        _logger.LogInformation("Loading existing songs for {FolderCount} folder(s).", folderIds.Count);

        var existingSongs = await _db.Songs
            .Where(s => folderIds.Contains(s.MusicFolderId))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Loaded {SongCount} existing song(s) from the database.", existingSongs.Count);

        var existingByPath = existingSongs.ToDictionary(
            s => NormalizePath(s.FilePath),
            s => s,
            comparer);

        var seenPaths = new HashSet<string>(comparer);

        int added = 0;
        int updated = 0;
        int deleted = 0;
        int filesScanned = 0;

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogDebug("Database transaction started for library sync.");

        foreach (var (folder, folderPath) in normalizedFolders)
        {
            _logger.LogInformation("Scanning folder: {FolderPath}", folderPath);

            foreach (var filePath in Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var extension = Path.GetExtension(filePath);
                if (!SupportedExtensions.Contains(extension))
                {
                    _logger.LogDebug("Skipping unsupported file: {FilePath}", filePath);
                    continue;
                }

                filesScanned++;

                var fullPath = NormalizePath(filePath);
                if (!seenPaths.Add(fullPath))
                {
                    _logger.LogDebug("Skipping duplicate path during sync: {FilePath}", fullPath);
                    continue;
                }

                var info = new FileInfo(fullPath);

                if (existingByPath.TryGetValue(fullPath, out var existingSong))
                {
                    var changed = false;

                    if (existingSong.FileSize != info.Length)
                    {
                        _logger.LogDebug("File size changed for {FilePath}: {OldSize} -> {NewSize}",
                            fullPath, existingSong.FileSize, info.Length);
                        existingSong.FileSize = info.Length;
                        changed = true;
                    }

                    if (existingSong.LastModifiedUtc != info.LastWriteTimeUtc)
                    {
                        _logger.LogDebug("Last modified time changed for {FilePath}.", fullPath);
                        existingSong.LastModifiedUtc = info.LastWriteTimeUtc;
                        changed = true;
                    }

                    if (!string.Equals(existingSong.FileName, info.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("File name changed for {FilePath}: {OldName} -> {NewName}",
                            fullPath, existingSong.FileName, info.Name);

                        existingSong.FileName = info.Name;
                        changed = true;

                        var derivedTitle = Path.GetFileNameWithoutExtension(info.Name);
                        var oldDerivedTitle = Path.GetFileNameWithoutExtension(Path.GetFileName(existingSong.FilePath));

                        if (string.IsNullOrWhiteSpace(existingSong.Title) ||
                            string.Equals(existingSong.Title, oldDerivedTitle, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogDebug("Auto-updating title for {FilePath} to {Title}", fullPath, derivedTitle);
                            existingSong.Title = derivedTitle;
                        }
                    }

                    if (changed)
                    {
                        updated++;
                        _logger.LogInformation("Updated song entry: {FilePath}", fullPath);
                    }
                    else
                    {
                        _logger.LogTrace("No changes detected for: {FilePath}", fullPath);
                    }
                }
                else
                {
                    var song = new Song
                    {
                        MusicFolderId = folder.Id,
                        FilePath = fullPath,
                        FileName = info.Name,
                        Title = Path.GetFileNameWithoutExtension(info.Name),
                        FileSize = info.Length,
                        LastModifiedUtc = info.LastWriteTimeUtc,
                        Duration = TimeSpan.Zero,
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.Songs.Add(song);
                    added++;

                    _logger.LogInformation("Added new song: {Title} ({FilePath})", song.Title, song.FilePath);
                }
            }

            folder.LastScannedAt = DateTime.UtcNow;
            _logger.LogInformation("Finished scanning folder: {FolderPath}", folderPath);
        }

        var missingSongs = existingSongs
            .Where(s => !seenPaths.Contains(NormalizePath(s.FilePath)))
            .ToList();

        if (missingSongs.Count > 0)
        {
            _logger.LogWarning("{Count} song(s) missing from disk will be deleted.", missingSongs.Count);

            foreach (var song in missingSongs)
            {
                _logger.LogWarning("Deleting missing song: {Title} ({FilePath})", song.Title, song.FilePath);
            }

            _db.Songs.RemoveRange(missingSongs);
            deleted = missingSongs.Count;
        }

        var savedChanges = await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Library sync complete. FoldersScanned={FoldersScanned}, FilesScanned={FilesScanned}, Added={Added}, Updated={Updated}, Deleted={Deleted}, SavedChanges={SavedChanges}",
            normalizedFolders.Count, filesScanned, added, updated, deleted, savedChanges);

        return new LibrarySyncResult(
            FoldersScanned: normalizedFolders.Count,
            FilesScanned: filesScanned,
            Added: added,
            Updated: updated,
            Deleted: deleted,
            SkippedFolders: folders.Count - normalizedFolders.Count
        );
    }

    private static string NormalizePath(string path)
    {
        var full = Path.GetFullPath(path);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}