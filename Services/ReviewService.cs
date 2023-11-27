using Server1001.Models;
using Server1001.Models.Views;

namespace Server1001.Services;

public interface IReviewService
{
    Task<bool> SaveReviewAsync(Review review);
    Task<List<HistoryView>> GetReviewsByUserIdAsync(Guid userId);
    Task<List<Guid>> GetReviewedSongIdsByUserAsync(Guid userId);
    Task<List<Review>> GetReviewsBySongIdAsync(Guid songId);
    Task<Review?> GetReviewByUserIdAndSongIdAsync(Guid userId, Guid songId);
}

public class ReviewService : IReviewService
{
    private ILogger<IReviewService> _logger;
    private IDynamoRepository _repository;

    public ReviewService(ILogger<IReviewService> logger, IDynamoRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public async Task<List<HistoryView>> GetReviewsByUserIdAsync(Guid userId)
    {
        var result = new List<HistoryView>();
        var reviews = await _repository.GetReviewsByUserIdAsync(userId);

        _logger.LogInformation("Fetched {count} reviews for user {id}", reviews.Count, userId);

        foreach (var review in reviews)
        {   
            if (review.Value.SongId == null)
                continue;

            Song? song = await _repository.GetSongByIdAsync(review.Value.SongId.Value);

            if (song != null)
            {
                var historyItem = new HistoryView(review.Value, song);
                result.Add(historyItem);
            }
        }
        CancellationToken t = CancellationToken.None;
        await Parallel.ForEachAsync(result, async (item, t) => { await UpdateSongStats(item); });

        return result;
    }

    public async Task<List<Guid>> GetReviewedSongIdsByUserAsync(Guid userId)
    {
        return await _repository.GetSongIdsReviewedByUserIdAsync(userId);
    }

    public async Task<Review?> GetReviewByUserIdAndSongIdAsync(Guid userId, Guid songId)
    {
        var userReviews = await _repository.GetReviewByUserIdAndSongIdAsync(userId, songId);
        foreach (var review in userReviews.Values)
        {
            if (review.SongId == songId)
            {
                return review;
            }
        }

        return null;
    }

    public async Task<List<Review>> GetReviewsBySongIdAsync(Guid songId)    
    {
        var result = await _repository.GetReviewsBySongIdAsync(songId);
        return result.Values.ToList<Review>();
    }

    public async Task<bool> SaveReviewAsync(Review review)
    {
        _ = await _repository.SaveReviewAsync(review);
        return true;
    }

    private async Task<HistoryView> UpdateSongStats(HistoryView historyItem)
    {
        var songReviews = await _repository.GetReviewsBySongIdAsync(historyItem.ReviewedSong.Id);
        double sum = 0, count = 0;
        if (songReviews != null)
        {
            foreach (Review r in songReviews.Values.ToList())
            {
                ++count;
                sum += r.StarRating;
            }
        }

        if (count > 0)
        {
            historyItem.ReviewAverage = Math.Round((sum / count), 2);
            historyItem.ReviewCount = Convert.ToInt32(count);
        }

        return historyItem;
    }
}