using Microsoft.AspNetCore.Mvc;
using EchoNet.Services;
using EchoNet.Models;
using EchoNet.ViewModels;
using EchoNet.Utils;

namespace EchoNet.Controllers;

public class SongsController : Controller
{
    private readonly ILogger<SongsController> _logger;
    private readonly ISongService _songService;
    private readonly AppDataJsonReader _appDataReader;

    public SongsController(ILogger<SongsController> logger, ISongService songService, AppDataJsonReader appDataReader)
    {
        _logger = logger;
        _songService = songService;
        _appDataReader = appDataReader;
    }

    public async Task<IActionResult> Index()
    {
        var songs = await _songService.GetAllSongsAsync();
        List<SongMetadata> metadataSongs = new List<SongMetadata>{};

        foreach (Song song in songs)
        {
            metadataSongs.Add(MetadataHelper.ReadSongMetadata(song));
        }

        return View(metadataSongs);
    }

    [HttpPost("Song/SaveState")]
    public ActionResult SaveState([FromBody] SongPageStateRequest request)
    {   
        _appDataReader.UpdateInMemory([(AppDataTarget.ViewType, request.viewType),(AppDataTarget.SortType, request.sortType)]);
        return Ok();
    }
}