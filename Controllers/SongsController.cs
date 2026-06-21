using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.ViewModels;
using EchoNet.Utils;

namespace EchoNet.Controllers;

public class SongsController : Controller
{
    private readonly ILogger<SongsController> _logger;
    private readonly IQueueManagerService _queueManager;
    private readonly AppDataJsonReader _appDataReader;

    public SongsController(ILogger<SongsController> logger, IQueueManagerService queueManager, AppDataJsonReader appDataReader)
    {
        _logger = logger;
        _queueManager = queueManager;
        _appDataReader = appDataReader;
    }

    public IActionResult Index()
    {
        List<Song> songs = _queueManager.GetQueue();
        _logger.LogDebug("Loading Songs Index view. Total songs in queue: {SongCount}", songs?.Count ?? 0);
        return View(songs);
    }

    public void ArrangeQueueOrder(string sortType)
    {
        PlayerState playerState = _queueManager.GetPlayerState();
        QueueState newQueueState;

        if (playerState.queueState == QueueState.Random) 
        {
            _logger.LogInformation("Arrange Queue Order skipped: queue is set to Random (Shuffle is on). Requested sort: {SortType}", sortType);
            return; // if shuffle is toggled don't update the queue
        }
        else if ( playerState.queueState.ToString().ToLower() == sortType)
        {
            _logger.LogInformation("Arrange Queue Order skipped: queue is set to same sort order. Requested sort: {SortType} Original sort: {SortType}", sortType, playerState.queueState);
            return; // if it was the sane sort type don't update
        }
        else
        {
            if (sortType == "newest") {newQueueState = QueueState.Newest;}
            else if (sortType == "oldest") {newQueueState = QueueState.Oldest;}
            else if (sortType == "az") {newQueueState = QueueState.AZ;}
            else {newQueueState = QueueState.ZA;}
        }

        PlayerState newPlayerState = new PlayerState
        {
            changeState = playerState.changeState, // same change state
            queueState = newQueueState
        };

        _queueManager.SetPlayerState(newPlayerState);
        _queueManager.SortQueue(newQueueState); // sort queue 
        _appDataReader.UpdateInMemory([(AppDataTarget.PlayerState, newPlayerState)]);

        _logger.LogInformation("Queue rearranged successfully from {OldQueueState} to {NewQueueState}", playerState.queueState, newQueueState);
    }

    [HttpPost("Song/SaveState")]
    public ActionResult SaveState([FromBody] SongPageStateRequest request)
    {   
        _appDataReader.UpdateInMemory([(AppDataTarget.ViewType, request.viewType),(AppDataTarget.SortType, request.sortType)]);
        ArrangeQueueOrder(request.sortType);
        return Ok();
    }
}