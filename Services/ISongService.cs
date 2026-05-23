using EchoNet.Models;
using EchoNet.ViewModels;

namespace EchoNet.Services;

public interface ISongService
{
    Task<bool> AddSongsAsync(List<Song> Songs);
    Task<List<Song>> GetAllSongsAsync();
    Task<Song?> GetSongByIdAsync(Guid id);
    Task<List<Song>> SearchByTitleAsync(string text);
    Task<List<Song>> SearchByGenreAsync(string genre);
    Task<List<Song>> SearchByArtistAsync(string artist);
    Task<List<Song>> GeneralSearchAsync(string term);
    Task<bool> DeleteSongAsync(Guid id);

    Task<LibrarySyncResult> SyncLibraryAsync(CancellationToken cancellationToken = default);
}