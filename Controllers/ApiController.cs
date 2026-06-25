using EchoNet.Services;
using EchoNet.Models;
using EchoNet.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EchoNet.Controllers;

[ApiController]
[Route("api/music")] // route for network devices
public class ApiController : ControllerBase
{
    private readonly ILogger<ApiController> _logger;
    private readonly IQueueManagerService _queueManager;
    private readonly string CoverArtDirectory;

    public ApiController(ILogger<ApiController> logger, IQueueManagerService queueManager, IWebHostEnvironment env)
    {
        _logger = logger;
        _queueManager = queueManager;
        CoverArtDirectory = Path.Combine(env.WebRootPath, "data", "cover");
    }

    // Accessible via: http://<Host-IP>:XXXX/api/music/songs
    [HttpGet("songs")]
    public IActionResult GetSongs()
    {
        _logger.LogDebug("LAN device requested the song list.");
        
        string baseUrl = $"{Request.Scheme}://{Request.Host}";
        var rawQueue = _queueManager.GetQueue(QueueType.Local);

        // Map internal Song models to a DTO so the receiver gets absolute network URLs
        var networkQueue = rawQueue.Select(song => new SongDTO
        {
            Id = song.Id,
            Title = song.Title,
            Artist = song.Artist,
            Album = song.Album,
            Duration = song.Duration,
            CreatedAt = song.CreatedAt,
            FormattedDuration = song.FormattedDuration,
            FormattedCreatedAt = song.FormattedCreatedAt,
            HasCoverArt = song.HasCoverArt,
            HostUrl = $"{baseUrl}/api/music"
        });

        return Ok(networkQueue);
    }

    // Accessible via: http://<Host-IP>:XXXX/api/music/stream/{id}
    [HttpGet("stream/{id:guid}")]
    public IActionResult StreamSong(Guid id)
    {
        _logger.LogDebug("Streaming request received for song ID: {SongID}", id);
        
        // Map the ID to a real file path on the host system
        Song? song = _queueManager.FindSongByIdFromQueue(id, QueueType.Local); 

        if (song is null)
        {
            _logger.LogError("Song ID: {SongID} pointed to a nonexisting file", id);
            return NotFound("The requested song could not be found on the server.");
        }

        // MIME type is hardcoded for now
        string contentType = "audio/mpeg"; // expects mp3 file 

        // Return the song file. 
        return PhysicalFile(song.FilePath, contentType, enableRangeProcessing: true);
    }

    // Accessible via: http://<Host-IP>:XXXX/api/music/coverArt/{id}/{size}
    [HttpGet("coverArt/{id:guid}/{size:int}")]
    public IActionResult StreamCoverArt(Guid id, int size)
    {
        _logger.LogDebug("CoverArt request received for song ID: {SongID}", id);

        // Map the ID to a real file path on the host system
        Song? song = _queueManager.FindSongByIdFromQueue(id,QueueType.Local); 

        if (song is null)
        {
            _logger.LogError("Song ID: {SongID} pointed to a nonexisting file", id);
            return NotFound("The requested coverArt could not be found on the server.");
        }

        if (!song.HasCoverArt)
        {
            _logger.LogError("Song ID: {SongID} does not have a cover art", id);
            return NotFound("The requested coverArt could not be found on the server.");
        }

        string CoverArt = Path.Combine(CoverArtDirectory, $"{id}_{size}.jpg");

        if (!System.IO.File.Exists(CoverArt))
        {
            _logger.LogError("Song ID: {SongID} does not have a cover art", id);
            return NotFound("The requested coverArt could not be found on the server.");
        }

        // MIME type is always jpg
        string contentType = "image/jpeg"; // expects jpg, jpeg, or jpe file

        // Return the image file 
        return PhysicalFile(CoverArt, contentType);
    }
}