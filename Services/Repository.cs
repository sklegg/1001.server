namespace Server1001.Services;

using System.Text.Json;
using System.Text.Json.Serialization;
using Server1001.Models;

public interface IRepository
{
    User? FindUserByEmail(string email);
    User? FindUserById (Guid userId);
    Guid CreateMagicLink(User user);
    User? FindUserByMagicLinkToken(Guid token);
    int AddUser(User newUser);

    Song? GetSongById(Guid songId);
    int GetSongDbCount();
    Dictionary<Guid, Song> GetSongs();

    Review SaveReview(Review review);
    Review GetReviewById(Guid reviewId);
    List<Review> GetUserReviews(Guid userId);
    List<Review> GetReviews(Guid songId);
}

public class Repository : IRepository
{
    private Dictionary<int, User> _users;
    private Dictionary<Guid, User> _links;
    private Dictionary<Guid, Song> _songs;
    private Dictionary<Guid, Review> _reviews; 

    public Repository() {
        _links = new Dictionary<Guid, User>();
        _users = new Dictionary<int, User>();
        _songs = new Dictionary<Guid, Song>();
        _reviews = new Dictionary<Guid, Review>();
    }

    public User? FindUserByEmail(string email)
    {
        foreach(var (key, value) in _users)
        {
            if (value.Email == email) {
                return value;
            }
        }

        return null;
    }

    public User? FindUserById (Guid userId)
    {
        foreach(var (key, value) in _users)
        {
            if (value.Id == userId) {
                return value;
            }
        }

        return null;
    }

    public int AddUser(User newUser) {
        var id = _users.Count + 1;
        _users.Add(id, newUser);
        return id;
    }

    public Guid CreateMagicLink(User user)
    {
        var token = Guid.NewGuid();
        _links.Add(token, user);
        return token;
    }

    public User? FindUserByMagicLinkToken(Guid token)
    {
        return _links[token];
    }

    public Song? GetSongById(Guid songId) 
    {
        return _songs[songId];
    }

    public int GetSongDbCount()
    {
        return _songs.Count;
    }

    public Dictionary<Guid, Song> GetSongs()
    {
        return _songs;
    }

    public Review SaveReview(Review review)
    {
        var id = Guid.NewGuid();
        _reviews.Add(id, review);
        return review; 
    }

    public Review GetReviewById(Guid reviewId)
    {
        return _reviews[reviewId];
    } 

    public List<Review> GetUserReviews(Guid userId)
    {
        List<Review> result = new();
        foreach (var r in _reviews)
        {
            if (r.Value.ReviewerId == userId) {
                result.Add(r.Value);
            }
        }
        return result;
    }    

    public List<Review> GetReviews(Guid songId)
    {
        List<Review> result = new();
        foreach (var r in _reviews)
        {
            if (r.Value.SongId == songId) {
                result.Add(r.Value);
            }
        }
        return result;        
    }
}