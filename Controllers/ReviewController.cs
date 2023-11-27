using Microsoft.AspNetCore.Mvc;

using Server1001.Models;
using Server1001.Services;
using Server1001.Shared.Authorization;
using Server1001.Shared;

namespace Server1001.Controllers;

[ApiController]
[Route("review")]
[Authorize]
public class ReviewController : ControllerBase
{
    private ILogger<ReviewController> _logger;
    private IReviewService _reviewService;
    private IUserService _userService;

    public ReviewController(ILogger<ReviewController> logger, IReviewService reviewService, IUserService userService){
        _logger = logger;
        _reviewService = reviewService;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviewsBySongId([FromQuery]Guid songId)
    {
        List<Review> result = await _reviewService.GetReviewsBySongIdAsync(songId);
        return Ok(result);
    }
    
    [HttpPost("submit")]
    public async Task<IActionResult> SubmitReview([FromBody]Review review)
    {
        User? user = (User?)HttpContext.Items["User"];
        if (user == null)
        {
            _logger.LogWarning(Events.NoUserContext, "User context is null or the userId is missing.");
            return NotFound();
        }

        // prevent a review with a rating out of range
        if (review.StarRating < 1 || review.StarRating > 5)
        {
            var error = new ErrorModel("Star rating out of range.", StatusCodes.Status400BadRequest);
            _logger.LogInformation("User {user} attempted to submit a rating of {star}", user.Id, review.StarRating);
            return Problem(error.ErrorMessage, null, error.StatusCode);
        }

        // disallow null comment
        if (review.Comment == null)
        {
            var error = new ErrorModel("No comment provided.", StatusCodes.Status400BadRequest);
            _logger.LogInformation("User {user} attempted to submit a blank comment", user.Id);
            return Problem(error.ErrorMessage, null, error.StatusCode);
        }

        // limit review comment length
        if (review.Comment != null && review.Comment.Length > 2000)
        {
            var error = new ErrorModel("Comment length exceeds limit of 2000 characters.", StatusCodes.Status400BadRequest);
            _logger.LogInformation("User {user} attempted to submit a comment with length {len}", user.Id, review.Comment.Length);
            return Problem(error.ErrorMessage, null, error.StatusCode);            
        }

        // make sure the user did not submit a review for this song already
        var existingReview = await _reviewService.GetReviewByUserIdAndSongIdAsync(user.Id, review.SongId.Value);        
        if (existingReview != null)
        {
            var error = new ErrorModel("You have already reviewed this song.", StatusCodes.Status409Conflict);
            _logger.LogInformation("User {user} attempted resubmit a rating for {song}", user.Id, review.SongId.Value);
            return Problem(error.ErrorMessage, null, error.StatusCode);
        }

        review.PostedDateTime = DateTime.UtcNow;

        if (review.SongId != null) {
            if (review.SongId != user.CurrentSongId)
            {
                var error = new ErrorModel("You can only review your current song.", StatusCodes.Status409Conflict);
                _logger.LogInformation("User {user} attempted submit a rating for a song other than their current song.", user.Id);
                return Problem(error.ErrorMessage, null, error.StatusCode);
            }
        }

        review.ReviewerId = user.Id;
        if (await _reviewService.SaveReviewAsync(review)) {
            _ = await _userService.GenerateRandomSongForUser(user);
        }

        return Created("/", review);
    }
}