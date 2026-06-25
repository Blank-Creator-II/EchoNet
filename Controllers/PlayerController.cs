using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.Utils;
using EchoNet.ViewModels;

namespace EchoNet.Controllers;

public class PlayerController : Controller
{
    private readonly IAudioService _audio;
    private readonly ILibScannerService _libScannerService;
    private readonly AppDataJsonReader _appDataReader;
    private readonly IQueueManagerService _queueManager;
    private readonly ILogger<PlayerController> _logger;

    public PlayerController(IAudioService audio, ILibScannerService libScanner, AppDataJsonReader appDataReader, IQueueManagerService queueManager, ILogger<PlayerController> logger)
    {
        _audio = audio;
        _libScannerService = libScanner;
        _appDataReader = appDataReader;
        _queueManager = queueManager;
        _logger = logger;
    }

    [HttpPost("Player/Play")]
    public async Task<IActionResult> Play([FromBody] Song _song)
    {
        if (_song == null)
        {
            _logger.LogWarning("Play request failed: Song was null.");
            return NotFound();
        }
        
        _logger.LogInformation("Play request received with ID: {SongId}", _song.Id);

        _audio.CurrentSongID = _song.Id;

        var filePath = _song.FilePath;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("Play request failed: File path was null or empty for Song ID: {SongId}", _song.Id);
            return BadRequest("No path provided");
        }

        try
        {
            await _audio.LoadAsync(filePath);
            await _audio.PlayAsync(_song);
            _logger.LogInformation("Successfully playing file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while attempting to load or play file: {FilePath}", filePath);
            return StatusCode(500, "An error occurred during playback.");
        }

        return Ok();
    }

    [HttpPost("Player/PlayRemote")]
    public async Task<IActionResult> PlayRemote([FromBody] RemotePlayRequest request)
    {
        if (request.songDTO == null)
        {
            _logger.LogWarning("Remote play request failed: Song was null.");
            return NotFound();
        }
        
        _logger.LogInformation("Remote play request received with ID: {SongId}", request.songDTO.Id);

        _audio.CurrentSongID = request.songDTO.Id;

        var streamUrl = $"{request.songDTO.HostUrl}/stream/{request.songDTO.Id}";

        try
        {
            await _audio.LoadRemoteAsync(streamUrl);
            await _audio.PlayAsync(request.songDTO.ToSong());
            _logger.LogInformation("Successfully playing stream: {FilePath}", streamUrl);

            // update queue from local to remote queue
            List<Song> remoteQueue = new List<Song>{};
            foreach (SongDTO songDTO in request.songDTOs)
            {
                remoteQueue.Add(songDTO.ToSong());
            }
            await _queueManager.GenerateQueue(QueueType.Remote,remoteQueue);
            _queueManager.SortQueue(QueueType.Remote, _appDataReader.Current.playerState.queueState, _appDataReader.Current.ShuffleSeed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while attempting to load or play stream: {FilePath}", streamUrl);
            return StatusCode(500, "An error occurred during playback.");
        }

        return Ok();
    }

    [HttpPost("Player/UpdateRemoteQueue")]
    public async Task<IActionResult> UpdateRemoteQueue([FromBody] List<SongDTO> updatedSongs)
    {
        if (updatedSongs == null)
        {
            return BadRequest("Song DTO array cannot be null.");
        }

        List<Song> remoteQueue = new List<Song>{};
        foreach (SongDTO songDTO in updatedSongs)
        {
            remoteQueue.Add(songDTO.ToSong());
        }
        bool success = await _queueManager.GenerateQueue(QueueType.Remote, remoteQueue);
        _queueManager.SortQueue(QueueType.Remote, _appDataReader.Current.playerState.queueState, _appDataReader.Current.ShuffleSeed);
        
        if (!success)
        {
            return StatusCode(500, "Error updating back-end remote context queue mappings.");
        }

        return Ok();
    }

    [HttpPost("Player/TogglePlay")]
    public async Task<IActionResult> TogglePlay()
    {
        var playing = _audio.IsPlaying;
        if (playing)
        {
            _audio.Pause();
            playing = false;   
        }
        else
        {
            await _audio.PlayAsync();
            playing = true;
        }

        _logger.LogInformation("Playback toggled. IsPlaying: {IsPlaying}", playing);
        
        return Ok(new
        {
            isPlaying = playing,
            isSeekable = _audio.IsSeekable
        });
    }

    [HttpPost("Player/Previous")]
    public async Task<ActionResult> Previous()
    {
        _logger.LogInformation("Skipping to previous track.");
        await _queueManager.PlayPreviousAsync();
        return Ok();
    }

    [HttpPost("Player/Next")]
    public async Task<ActionResult> Next()
    {
        _logger.LogInformation("Skipping to next track.");
        await _queueManager.PlayNextAsync();
        return Ok();
    }

    [HttpPost("Player/Loop")]
    public ActionResult Loop()
    {
        PlayerState playerState = _appDataReader.Current.playerState;
        ChangeState newchangeState;

        int _newState = 0; // 0 = NoLoop, 1 = Loop, 2 = LoopOnce

        if (playerState.changeState == ChangeState.NoLoop)
        {
            newchangeState = ChangeState.Loop;
            _newState = 1;
        }
        else if (playerState.changeState == ChangeState.Loop)
        {
            newchangeState = ChangeState.LoopOnce;
            _newState = 2;
        }
        else
        {
            newchangeState = ChangeState.NoLoop;
        }
        
        PlayerState newPlayerState = new PlayerState
        {
            changeState = newchangeState,
            queueState = playerState.queueState // same state as before
        };
        
        _queueManager.SetPlayerState(newPlayerState);
        _appDataReader.UpdateInMemory([(AppDataTarget.PlayerState, newPlayerState)]);

        _logger.LogInformation("Loop mode changed from {OldState} to {NewState}", playerState.changeState, newchangeState);

        return Ok(new {newState = _newState});
    }

    [HttpGet("Player/Status")]
    public IActionResult Status()
    {
        _logger.LogDebug("Status polled. Current ID: {SongId}, Time: {CurrentTime}, Duration: {Duration}, IsPlaying: {IsPlaying}, IsSeekable: {IsSeekable}, Volume: {Volume}%", _audio.CurrentSongID, _audio.CurrentTime.TotalSeconds, _audio.Duration.TotalSeconds, _audio.IsPlaying, _audio.IsSeekable, _audio.Volume);

        return Json(new
        {
            id = _audio.CurrentSongID,
            currentTime = _audio.CurrentTime.TotalSeconds,
            duration = _audio.Duration.TotalSeconds,
            isPlaying = _audio.IsPlaying,
            isSeekable = _audio.IsSeekable,
            volume = _audio.Volume
        });
    }

    [HttpPost("Player/Seek")]
    public IActionResult Seek([FromBody] SeekRequest request)
    {
        _logger.LogDebug("Seeking playback to position: {Position} seconds", request.Position);
        _audio.Seek(TimeSpan.FromSeconds(request.Position));
        return Ok();
    }

    [HttpPost("Player/Volume")]
    public IActionResult Volume([FromBody] VolumeRequest request)
    {
        _logger.LogDebug("Volume adjustment request received: {Volume}", request.Volume);
        _audio.SetVolume(request.Volume);
        return Ok();
    }

    [HttpPost("Player/FolderPicker")]
    public async Task<IActionResult> FolderPicker([FromBody] List<string> paths, CancellationToken cancellationToken)
    {
        if (paths == null || paths.Count == 0)
        {
            _logger.LogWarning("FolderPicker calling failed: No paths were selected.");
            return BadRequest(new
            {
                success = false,
                message = "No folders selected."
            });
        }

        _logger.LogInformation("Starting library scan for {FolderCount} directories...", paths.Count);

        var success = await _libScannerService.ScanFoldersAsync(paths, cancellationToken);

        if (!success)
        {
            _logger.LogWarning("Library scanner returned a failure status for requested paths.");
            return BadRequest(new
            {
                success = false,
                message = "Library setup failed."
            });
        }

        await _queueManager.GenerateQueue(QueueType.Local);
        _queueManager.SortQueue(QueueType.Local, _appDataReader.Current.playerState.queueState, _appDataReader.Current.ShuffleSeed);

        _logger.LogInformation("Library scan successfully completed and queue generated.");

        return Ok(new
        {
            success = true,
            message = "Library scan completed."
        });
    }

    [HttpPost("Player/ToggleShuffle")]
    public ActionResult ToggleShuffle()
    {
        PlayerState playerState = _appDataReader.Current.playerState;
        QueueState newQueueState;

        bool _IsShuffled = true;

        if (playerState.queueState == QueueState.Random)
        {
            if (_appDataReader.Current.SortType == "newest") {newQueueState = QueueState.Newest;}
            else if (_appDataReader.Current.SortType == "oldest") {newQueueState = QueueState.Oldest;}
            else if (_appDataReader.Current.SortType == "az") {newQueueState = QueueState.AZ;}
            else {newQueueState = QueueState.ZA;}

            _IsShuffled = false;
        }
        else
        {
            newQueueState = QueueState.Random;
        }

        PlayerState newPlayerState = new PlayerState
        {
            changeState = playerState.changeState, // same state as before
            queueState = newQueueState
        };

        _queueManager.SetPlayerState(newPlayerState);
        int seed = Random.Shared.Next(int.MinValue, int.MaxValue); // generate seed for shuffling between -2,147,483,648 and 2,147,483,647
        _queueManager.SortQueue(QueueType.Local, newQueueState, seed); // Sort local queue
        _queueManager.SortQueue(QueueType.Remote, newQueueState, seed); // Sort remote queue

        _appDataReader.UpdateInMemory([(AppDataTarget.PlayerState, newPlayerState),(AppDataTarget.ShuffleSeed, seed)]);
        _logger.LogInformation("Shuffle toggled. Shuffled: {IsShuffled}. Queue State: {QueueState}. Seed assigned: {Seed}", _IsShuffled, newQueueState, seed);

        return Ok(new {IsShuffled = _IsShuffled});
    }

    [HttpPost("Player/SaveState")]
    public ActionResult SaveState([FromBody] Song song)
    {   
        _appDataReader.UpdateInMemory([(AppDataTarget.Song, song)]);
        return Ok();
    }
}