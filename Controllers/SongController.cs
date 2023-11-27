using Microsoft.AspNetCore.Mvc;

using Server1001.Services;
using Server1001.Shared.Authorization;
using Server1001.Shared;

namespace Server1001.Controllers;

[ApiController]
[Route("song")]
[Authorize]
public class SongController : ControllerBase
{
    private ILogger<SongController> _logger;
    private ISongService _songService;

    public SongController(ILogger<SongController> logger, ISongService songService)
    {
        _logger = logger;
        _songService = songService;
    }

    [HttpGet]
    public IActionResult GetSongs() {
        var songs = _songService.GetAllSongsAsync();

        return Ok(songs);
    }

    [HttpGet("{songId}")]
    public async Task<IActionResult> GetSongByIdAsync(string songId)
    {
        if (string.IsNullOrEmpty(songId)) {
            _logger.LogWarning(Events.SongFetchError, "Null or empty song Id");
            NotFound();
        }

        try {
            Guid id = Guid.Parse(songId);
            var song = await _songService.GetSongByIdAsync(id);
            return Ok(song);
        } catch {
            _logger.LogWarning(Events.GuidParseError, "An error occurred parsing a songId.");
            var error = new ErrorModel("Bad Request", StatusCodes.Status500InternalServerError);
            Problem(error.ErrorMessage, null, error.StatusCode);
        }

        _logger.LogError("Something weird happened.");
        var weirdError = new ErrorModel("No idea", StatusCodes.Status500InternalServerError);
        return Problem(weirdError.ErrorMessage, null, weirdError.StatusCode);
    }
}