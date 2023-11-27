using System.Web;
using Server1001.Models;
using Server1001.Models.Views;

namespace Server1001.Services;

public interface IUserService
{
    Task<User?> CreateNewUserAsync(string email);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<bool> GenerateMagicLinkAsync(User user);
    Task SendNotFoundEmail(string email);
    User? ValidateMagicLinkToken(Guid token);
    Task<Guid> GenerateRandomSongForUser(User user);
    Task UpdatePreferences(User user, UserPrefs prefs);
    Task<string> CreateShareLink(string userId);
    Task<Summary> GetSummaryByLink(string linkValue);
    Task<string?> GetLinkByUserId(string userId);
}

public class UserService : IUserService
{
    private IDynamoRepository _repository;
    private ISongService _songService;
    private IEmailService _emailService;
    private IReviewService _reviewService;

    public UserService (IDynamoRepository repository, IReviewService reviewService, ISongService songService, IEmailService emailService)
    {
        _repository = repository;
        _songService = songService;
        _emailService = emailService;
        _reviewService = reviewService;
    }

    public async Task<User?> CreateNewUserAsync(string email)
    {
        User? newUser = new User(Guid.NewGuid(), email);
        newUser.CurrentSongId = await GenerateRandomSongForUser(newUser).ConfigureAwait(false);
        newUser.ActiveFlag = true;
        newUser.Created = DateTime.UtcNow;
        newUser.Preferences = new UserPrefs { ReviewSubmitAction = AfterSubmit.SongPage };
        newUser = await _repository.CreateNewUserAsync(newUser).ConfigureAwait(false);

        if (newUser != null)
            _ = await GenerateMagicLinkAsync(newUser).ConfigureAwait(false);

        return newUser;
    }

    public async Task<User?> GetUserByEmailAsync(string email) 
    {
        return await _repository.FindUserByEmailAsync(email).ConfigureAwait(false);
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        var user = await _repository.FindUserByIdAsync(userId).ConfigureAwait(false);
        if (user != null && user.CurrentSongId == null)
        {
            _ = GenerateRandomSongForUser(user);
        }
        
        return user;
    }

    public async Task<bool> GenerateMagicLinkAsync(User user)
    {
        Guid magicToken = _repository.CreateMagicLink(user);

        if (user.Email == null || user.ActiveFlag != true)
            return false;

        return await _emailService.SendMagicTokenEmailAsync(user.Email, magicToken.ToString());
    }

    public async Task SendNotFoundEmail(string email)
    {
        await _emailService.SendNotFoundEmail(email).ConfigureAwait(false);
    }

    public User? ValidateMagicLinkToken(Guid token)
    {
        return _repository.FindUserByMagicLinkToken(token);
    } 

    public async Task<Guid> GenerateRandomSongForUser(User user)
    {
        // get All Song IDs
        List<Guid> allSongIds = await _songService.GetAllSongIdsAsync().ConfigureAwait(false);

        // get all Song IDs from current user's reviews
        List<Guid> alreadyReviewed = await _reviewService.GetReviewedSongIdsByUserAsync(user.Id).ConfigureAwait(false);

        var unreviewed = allSongIds.Except(alreadyReviewed).ToList<Guid>();

        // generate random int from 0 to Count. use .ElementAt(random) to get the next Song
        int random = new Random().Next(0, unreviewed.Count);
        Guid nextSongId = unreviewed.ElementAt(random);

        user.CurrentSongId = nextSongId;
        user = await _repository.UpdateUsersNextSongAsync(user, nextSongId).ConfigureAwait(false);

        return nextSongId;
    }

    public async Task UpdatePreferences(User user, UserPrefs prefs)
    {
        await _repository.UpdateUserPreferencesAsync(user, prefs).ConfigureAwait(false);
    }

    public async Task<string> CreateShareLink(string userId)
    {
        string hex = RandomHexValue();
        string linkValue = HttpUtility.UrlEncode(hex);
        return await _repository.CreateShareLink(userId, linkValue);
    }

    public async Task<Summary> GetSummaryByLink(string linkValue)
    {
        Summary result = new();
        string userId = await _repository.GetUserIdByLinkValue(linkValue).ConfigureAwait(false);
        List<HistoryView> reviews = await _reviewService.GetReviewsByUserIdAsync(Guid.Parse(userId)).ConfigureAwait(false);

        double count = 0, sum = 0;
        foreach (var history in reviews)
        {
            count += 1;
            sum += history.Review.StarRating;
            if (history.Review.StarRating == 1)
                result.Duds.Add(history.ReviewedSong);
            if (history.Review.StarRating == 5)
                result.Bangers.Add(history.ReviewedSong);                
        }

        if (count > 0)
        {
            result.Count = Convert.ToInt32(count);
            result.Average = sum / count;
            result.Average = Math.Round(result.Average, 2);
        }

        return result;
    }

    public async Task<string?> GetLinkByUserId(string userId)
    {
        return await _repository.FindShareLinkByUser(userId);
    }    

    private string RandomHexValue()
    {
        Random r = new Random();
        byte[] buffer = new byte[12];
        r.NextBytes(buffer);
        return System.Convert.ToBase64String(buffer).Replace("/","_").Replace("+","Z");
    }
}