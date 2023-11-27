using Microsoft.AspNetCore.Mvc;

using Server1001.Shared.Authorization;
using Server1001.Models;
using Server1001.Services;
using Server1001.Shared;
using Server1001.Models.Views;

namespace Server1001.Controllers;

[Authorize]
[ApiController]
[Route("user")]
public class UserController : ControllerBase
{
    private ILogger<UserController> _logger;
    private IUserService _userService;
    private IReviewService _reviewService;
    private ICustomConfiguration _config;
    private IJwtUtils _jwt;

    public UserController(ILogger<UserController> logger, IUserService userService, IReviewService reviewService, IJwtUtils jwtUtils, ICustomConfiguration config)
    {
        _logger = logger;
        _userService = userService;
        _reviewService = reviewService;
        _config = config;
        _jwt = jwtUtils;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetUser() 
    {
        User? contextUser = (User?)HttpContext.Items["User"];
        if (contextUser == null){
            return NotFound();
        }

        var user = await _userService.GetUserByIdAsync(contextUser.Id).ConfigureAwait(false);
        return Ok(user);
    }

    [HttpGet("prefs")]
    public async Task<IActionResult> GetPreferences()
    {
        User? contextUser = (User?)HttpContext.Items["User"];
        if (contextUser == null){
            return NotFound();
        }

        User? user = await _userService.GetUserByIdAsync(contextUser.Id).ConfigureAwait(false);
        if (user != null)
            return Ok(user.Preferences);
        return NotFound();
    }

    [HttpPut("prefs")]
    public async Task<IActionResult> UpdatePreferences([FromBody]UserPrefs prefs)
    {
        User? contextUser = (User?)HttpContext.Items["User"];
        if (contextUser == null){
            return NotFound();
        }

        await _userService.UpdatePreferences(contextUser, prefs).ConfigureAwait(false);
        return await GetUser();
    }

    [HttpGet("me/reviews")]
    public async Task<IActionResult> GetUserReviews() {
        User? contextUser = (User?)HttpContext.Items["User"];
        if (contextUser == null){
            _logger.LogWarning(Events.NoUserContext, "User context is null or the userId is missing.");
            return NotFound();
        }

        var result = new List<HistoryView>(); 
        result = await _reviewService.GetReviewsByUserIdAsync(contextUser.Id).ConfigureAwait(false);

        return Ok(result);
    }    

    [HttpGet("magic")]
    [AllowAnonymous]
    public IActionResult MagicLink([FromQuery] string link) {
        if (Guid.TryParse(link, out Guid token))
        {
            User? user = _userService.ValidateMagicLinkToken(token);
            
            if (user != null) {
                var options = new CookieOptions {
                    HttpOnly = false,
                    Secure = false,
                    IsEssential = true,
                    MaxAge = TimeSpan.MaxValue
                };

                var jwt = _jwt.GenerateToken(user);

                Response.Cookies.Append("1001", jwt, options);
                HttpContext.Response.Headers.Add("access-control-expose-headers","Set-Cookie");
            } else {
                _logger.LogWarning(Events.UserNotFound, "Magic link value {magic} did not match a user.", link);
            }
        } else {
            _logger.LogWarning(Events.GuidParseError, "A magic link with the value {magic} could not be parsed.", link);
        }

        return Redirect(_config.WebBaseUrl);
    }


    [HttpPost("create")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateUser([FromBody]LoginForm form) {
        // validate that user exists with the email in the request body
        if (form.Email == null) {
            _logger.LogWarning(Events.NoEmailInLogin, "A CreateUser request was submitted without an email address.");
            return NotFound();
        }

        var user = await _userService.GetUserByEmailAsync(form.Email).ConfigureAwait(false);
        
        // if the user is not in the database, create it
        if (user == null)
        {
            User? newUser = await _userService.CreateNewUserAsync(form.Email).ConfigureAwait(false);
            return Created("/", newUser);
        }

        var error = new ErrorModel("User already exists", StatusCodes.Status409Conflict);
        _logger.LogWarning(Events.UserAlreadyExists, "User {email} attempted to create an existing account.", form.Email);
        return Problem(error.ErrorMessage, null, error.StatusCode);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody]LoginForm form) {
        // validate that user exists with the email in the request body
        if (form.Email == null) {
            _logger.LogWarning(Events.NoEmailInLogin, "A Login request was submitted without an email address.");
            return NotFound();
        }

        var user = await _userService.GetUserByEmailAsync(form.Email).ConfigureAwait(false);
        
        // if the user is not in the database, return 404
        if (user == null)
        {
            _logger.LogWarning(Events.UserNotFound, "User {email} attempted to login but no User exists.", form.Email);

            // send a friendly email
            await _userService.SendNotFoundEmail(form.Email).ConfigureAwait(false);

            return NotFound();
        }

        // if it does, generate a magic link token and save it to the database
        var linkStatus = await _userService.GenerateMagicLinkAsync(user);

        // create an email with the magic link and queue it to send
        if (linkStatus)
        {
            return Accepted();
        }

        _logger.LogCritical("How did this happen?");
        var error = new ErrorModel("User already exists", StatusCodes.Status500InternalServerError);
        return Problem(error.ErrorMessage, null, error.StatusCode);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("1001");
        HttpContext.Response.Headers.Add("access-control-expose-headers","Set-Cookie");
        return Redirect(_config.WebBaseUrl);
    }


    [HttpPost("sharelink")]
    public async Task<IActionResult> CreateShareLink()
    {
        User? contextUser = (User?)HttpContext.Items["User"];
        if (contextUser == null){
            _logger.LogWarning(Events.NoUserContext, "User context is null or the userId is missing.");
            return NotFound();
        }

        // look up an existing link for this user
        string? existingLink = await _userService.GetLinkByUserId(contextUser.Id.ToString());
        if (existingLink != null)
            return Ok(existingLink);

        var linkValue = await _userService.CreateShareLink(contextUser.Id.ToString()).ConfigureAwait(false);
        return Ok(linkValue);        
    }

    [HttpGet("summary/{linkValue}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetShareSummaryByLinkValue(string linkValue)
    {
        Summary s = await _userService.GetSummaryByLink(linkValue).ConfigureAwait(false);
        return Ok(new SummaryView(s));
    }

}