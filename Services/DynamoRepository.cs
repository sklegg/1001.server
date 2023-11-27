namespace Server1001.Services;

using System.Collections.Concurrent;

using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

using Server1001.Models;
using Server1001.Shared;

public interface IDynamoRepository
{
    Task<bool> HealthCheck ();
    
    Task<User?> CreateNewUserAsync (User newUser);
    Task<User?> FindUserByEmailAsync (string email);
    Task<User?> FindUserByIdAsync (Guid userId);
    Task<User> UpdateUsersNextSongAsync (User user, Guid nextSongId);
    Guid CreateMagicLink (User user);
    User? FindUserByMagicLinkToken (Guid token);
    void DeleteMagicLink (Guid token);
    Task<User> UpdateUserPreferencesAsync(User user, UserPrefs prefs);
    Task<string> CreateShareLink(string userId, string linkValue);
    Task<string> GetUserIdByLinkValue(string linkValue);
    Task<string?> FindShareLinkByUser(string userId);

    Task<Review> SaveReviewAsync(Review review);
    Task<Review> GetReviewByIdAsync(Guid reviewId);
    Task<Dictionary<Guid, Review>> GetReviewsByUserIdAsync(Guid userId);
    Task<List<Guid>> GetSongIdsReviewedByUserIdAsync(Guid userId);
    Task<Dictionary<Guid, Review>> GetReviewsBySongIdAsync(Guid songId);
    Task<Dictionary<Guid, Review>> GetReviewByUserIdAndSongIdAsync(Guid userId, Guid songId);

    Task<Dictionary<Guid, Song>> GetSongsAsync();
    Task<Song?> GetSongByIdAsync(Guid songId);
    Task<int> GetSongDbCountAsync();
}


public class DynamoRepository : IDynamoRepository
{

    private ILogger<IDynamoRepository> _logger;

    private Tables _tables;
    private Dictionary<Guid, Song> _songs;
    private AmazonDynamoDBClient _client;
    private ConcurrentDictionary<Guid, User> _links;

    public DynamoRepository (ILogger<IDynamoRepository> logger, ICustomConfiguration config)
    {
        _logger = logger;
        _songs = new Dictionary<Guid, Song>();
        _links = new();
        
        AmazonDynamoDBConfig clientConfig = new AmazonDynamoDBConfig();
        clientConfig.RegionEndpoint = RegionEndpoint.USWest2;
        AWSCredentials creds = new BasicAWSCredentials(config.DatabaseKeyId, config.DatabaseKeySecret);
        _client = new AmazonDynamoDBClient(creds, clientConfig);
        _tables = new Tables(config.DeployEnv);

    }

    public async Task<bool> HealthCheck ()
    {
        try {
            var tables = await _client.ListTablesAsync();
            return tables.TableNames.Count > 0;
        } catch (InternalServerErrorException) {
            _logger.LogCritical(Events.DatabaseFailure, "Database health check failed.");
            return false;
        }
    }

#region User methods
    
    public async Task<User?> FindUserByEmailAsync (string email)
    {
        try {
            var queryRequest = new QueryRequest
            {
                TableName = _tables.Users,
                IndexName = "email-index",
                KeyConditionExpression = "email = :email",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":email", new AttributeValue { S = email }}}
            };

            var queryResponse = await _client.QueryAsync(queryRequest);
            var item = queryResponse.Items.FirstOrDefault();
            if (item == null) return null;
            return UserFromResult(item);
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return null;
        }
    }

    public async Task<User?> FindUserByIdAsync (Guid userId)
    {
        try {
            var keyAttribute = new AttributeValue(userId.ToString());
            var exp = new Dictionary<string, AttributeValue>();
            exp["userId"] =  keyAttribute;
            var response = await _client.GetItemAsync(_tables.Users, exp);
            if (response.Item.Count == 0) return null;
            return UserFromResult(response.Item);
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return null;
        }        
    }

    public async Task<User?> CreateNewUserAsync (User newUser)
    {
        try {
            if (newUser.Id == null) newUser.Id = Guid.NewGuid();
            var userResult = ResultFromUser(newUser);
            var response = await _client.PutItemAsync(_tables.Users, userResult);
            return newUser;
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return null;
        }          
    }

    public async Task<User> UpdateUsersNextSongAsync (User user, Guid nextSongId)
    {
        try {
            user.CurrentSongId = nextSongId;
            var updateRequest = new UpdateItemRequest
            {
                TableName = _tables.Users,
                Key = new Dictionary<string, AttributeValue>() { { "userId", new AttributeValue { S = user.Id.ToString() } } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>() {
                    {":nextSongId", new AttributeValue { S = nextSongId.ToString() }}},
                UpdateExpression = "SET currentSongId = :nextSongId"            
            };

            var response = await _client.UpdateItemAsync(updateRequest);
            return user;
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return user;
        }  
    }

    public Guid CreateMagicLink (User user)
    {
        Guid token = Guid.NewGuid();
        _links.TryAdd(token, user);
        return token;
    }

    public User? FindUserByMagicLinkToken (Guid token)
    {
        if (_links.TryGetValue(token, out User? result))
        {
            return result;
        }
        return null;
    }

    public void DeleteMagicLink (Guid token)
    {
        _links.Remove(token, out User? u);
    }

    public async Task<User> UpdateUserPreferencesAsync(User user, UserPrefs prefs)
    {
        try {
            user.Preferences = prefs;
            var updateRequest = new UpdateItemRequest
            {
                TableName = _tables.Users,
                Key = new Dictionary<string, AttributeValue>() { { "userId", new AttributeValue { S = user.Id.ToString() } } },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>() {
                    {":prefAfterSubmit", new AttributeValue { S = prefs.ReviewSubmitAction }}},
                UpdateExpression = "SET prefAfterSubmit = :prefAfterSubmit"            
            };

            var response = await _client.UpdateItemAsync(updateRequest);
            return user;            
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return user;
        }   
    }

    public async Task<string> CreateShareLink(string userId, string linkValue)
    {
        try
        {
            var linkItem = new Dictionary<string, AttributeValue>();
            linkItem["userId"] = new AttributeValue(userId);
            linkItem["linkValue"] = new AttributeValue(linkValue);

            var response = await _client.PutItemAsync(_tables.Links, linkItem);
            return linkValue;
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return "";
        } 
    }

    public async Task<string> GetUserIdByLinkValue(string linkValue)
    {
        try {
            var queryRequest = new QueryRequest
            {
                TableName = _tables.Links,
                IndexName = "linkValue-index",
                KeyConditionExpression = "linkValue = :linkValue",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":linkValue", new AttributeValue { S = linkValue }}}
            };

            var queryResponse = await _client.QueryAsync(queryRequest);
            var item = queryResponse.Items.FirstOrDefault();
            if (item == null) return string.Empty;
            return item["userId"].S;
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return string.Empty;
        }        
    }

    public async Task<string?> FindShareLinkByUser(string userId)
    {
        try {
            var keyAttribute = new AttributeValue(userId.ToString());
            var exp = new Dictionary<string, AttributeValue>();
            exp["userId"] =  keyAttribute;
            var response = await _client.GetItemAsync(_tables.Links, exp);
            if (response.Item.Count == 0) return null;
            return response.Item["linkValue"].S;
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return null;
        } 
    }

#endregion


#region Review Methods
    public async Task<Review> SaveReviewAsync(Review review)
    {
        try {
            if (review.ReviewId == null) review.ReviewId = Guid.NewGuid();
            var newReview = ResultFromReview(review);
            var response = await _client.PutItemAsync(_tables.Reviews, newReview);
            return review;
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return review;
        }              
    }

    public async Task<Review> GetReviewByIdAsync(Guid reviewId)
    {
        try {
            var keyAttribute = new AttributeValue("reviewId");
            var exp = new Dictionary<string, AttributeValue>();
            exp[reviewId.ToString()] =  keyAttribute;
            var response = await _client.GetItemAsync(_tables.Reviews, exp);
            return ReviewFromResult(response.Item);
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return null;
        }              
    } 

    public async Task<Dictionary<Guid, Review>> GetReviewsByUserIdAsync(Guid userId)
    {
        try {
            var queryRequest = new QueryRequest
            {
                TableName = _tables.Reviews,
                IndexName = "userId-index",
                KeyConditionExpression = "userId = :userId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":userId", new AttributeValue { S = userId.ToString() }}}
            };     

            var queryResponse = await _client.QueryAsync(queryRequest);
            List<Dictionary<string, AttributeValue>> items = queryResponse.Items;

            ConcurrentDictionary<Guid, Review> result = new();

            Parallel.ForEach(items, song =>
            {
                var tempReview = ReviewFromResult(song);
                result.TryAdd(tempReview.ReviewId!.Value, tempReview);
            });
            
            return new Dictionary<Guid, Review>(result, result.Comparer);
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return null;
        }  
    }

    public async Task<List<Guid>> GetSongIdsReviewedByUserIdAsync(Guid userId)
    {
        try {
            var queryRequest = new QueryRequest
            {
                TableName = _tables.Reviews,
                IndexName = "userId-index",
                KeyConditionExpression = "userId = :userId",
                ProjectionExpression = "songId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":userId", new AttributeValue { S = userId.ToString() }}}
            };     

            var queryResponse = await _client.QueryAsync(queryRequest);
            List<Dictionary<string, AttributeValue>> items = queryResponse.Items;
            ConcurrentBag<Guid> result = new();

            Parallel.ForEach(items, i =>
            {
                result.Add(Guid.Parse(i["songId"].S));
            });

            return result.ToList<Guid>();
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return null;
        }  
    }

    public async Task<Dictionary<Guid, Review>> GetReviewsBySongIdAsync(Guid songId)
    {
        try {
            var queryRequest = new QueryRequest
            {
                TableName = _tables.Reviews,
                IndexName = "songId-index",
                KeyConditionExpression = "songId = :songId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":songId", new AttributeValue { S = songId.ToString() }}}
            };

            var queryResponse = await _client.QueryAsync(queryRequest);
            List<Dictionary<string, AttributeValue>> items = queryResponse.Items;

            ConcurrentDictionary<Guid, Review> result = new();

            Parallel.ForEach(items, song =>
            {
                var tempReview = ReviewFromResult(song);
                result.TryAdd(tempReview.ReviewId!.Value, tempReview);
            });
            
            return new Dictionary<Guid, Review>(result, result.Comparer);
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return null;
        }              
    }

    public async Task<Dictionary<Guid, Review>> GetReviewByUserIdAndSongIdAsync(Guid userId, Guid songId)
    {
        try
        {
            var queryRequest = new QueryRequest
            {
                TableName = _tables.Reviews,
                IndexName = "userIdsongId-index",
                KeyConditionExpression = "userIdsongId = :userIdSongId",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    {":userIdSongId", new AttributeValue { S = userId.ToString() + "_" + songId.ToString() }}}
            };

            var queryResponse = await _client.QueryAsync(queryRequest);
            List<Dictionary<string, AttributeValue>> items = queryResponse.Items;

            ConcurrentDictionary<Guid, Review> result = new();

            Parallel.ForEach(items, song =>
            {
                var tempReview = ReviewFromResult(song);
                result.TryAdd(tempReview.ReviewId!.Value, tempReview);
            });
            
            return new Dictionary<Guid, Review>(result, result.Comparer);            

        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed.");
            return null;
        } 
    }
#endregion



#region Song Methods

    public async Task<Dictionary<Guid, Song>> GetSongsAsync()
    {
        await PopulateLocalSongsAsync();
        return _songs;
    }

    public async Task<Song?> GetSongByIdAsync(Guid songId)
    {
        await PopulateLocalSongsAsync();
        return _songs[songId];
    }

    public async Task<int> GetSongDbCountAsync()
    {
        await PopulateLocalSongsAsync();
        return _songs.Count;
    }

#endregion

    
#region Local Methods

    private Review ReviewFromResult(Dictionary<string, AttributeValue> attributeList)
    {
        string reviewId = attributeList["reviewId"].S;
        string reviewerId = attributeList["userId"].S;
        string songId = attributeList["songId"].S;
        int starRating = int.Parse(attributeList["starRating"].S);
        string comment = attributeList["comment"].S;
        string postedDateTime = attributeList["postedDateTime"].S;
        
        Review reviewResult = new Review();
        reviewResult.ReviewId = Guid.Parse(reviewId);
        reviewResult.Comment = comment;
        reviewResult.StarRating = starRating;
        reviewResult.SongId = Guid.Parse(songId);
        reviewResult.ReviewerId = Guid.Parse(reviewerId);
        reviewResult.PostedDateTime = DateTime.Parse(postedDateTime);
        
        return reviewResult;
    }

    private Dictionary<string, AttributeValue> ResultFromReview(Review review)
    {
        var result = new Dictionary<string, AttributeValue>();
        result["reviewId"] = new AttributeValue(review.ReviewId.ToString());
        result["userId"] = new AttributeValue(review.ReviewerId.ToString());
        result["songId"] = new AttributeValue(review.SongId.ToString());
        result["starRating"] = new AttributeValue(review.StarRating.ToString());
        result["comment"] = new AttributeValue(review.Comment);
        result["postedDateTime"] = new AttributeValue(review.PostedDateTime.ToString());
        result["userIdsongId"] = new AttributeValue(result["userId"].S + "_" + result["songId"].S);

        return result;
    }

    private User UserFromResult(Dictionary<string, AttributeValue> attributeList)
    {
        string userId = attributeList["userId"].S;
        string email = attributeList["email"].S;
        string currentSongId = attributeList["currentSongId"].S;
        string active = attributeList["active"].S;
        string created = attributeList["created"].S;
        string prefAfterSubmit = AfterSubmit.SongPage;
        if (attributeList.ContainsKey("prefAfterSubmit"))
            prefAfterSubmit = attributeList["prefAfterSubmit"].S;

        User userResult = new User(Guid.Parse(userId), email);
        userResult.CurrentSongId = Guid.Parse(currentSongId);
        userResult.ActiveFlag = bool.Parse(active);
        userResult.Created = DateTime.Parse(created);
        UserPrefs prefs = new UserPrefs{ ReviewSubmitAction = prefAfterSubmit };
        userResult.Preferences = prefs;

        return userResult;
    }

    private Dictionary<string, AttributeValue> ResultFromUser (User user)
    {
        var result = new Dictionary<string, AttributeValue>();

        result["userId"] = new AttributeValue(user.Id.ToString());
        result["email"] = new AttributeValue(user.Email);
        result["currentSongId"] = new AttributeValue(user.CurrentSongId.ToString());
        result["active"] = new AttributeValue(user.ActiveFlag.ToString());
        result["created"] = new AttributeValue(user.Created.ToString());
        result["prefAfterSubmit"] = new AttributeValue(user.Preferences.ReviewSubmitAction);

        return result;
    }    


    private Song SongFromResult(Dictionary<string, AttributeValue> attributeList)
    {
        string songId = attributeList["songId"].S;
        string artistName = attributeList["artistName"].S;
        string songTitle = attributeList["songTitle"].S;
        string spotifyArtLink = attributeList["spotifyArtLink"].S;
        string spotifyLink = attributeList["spotifyLink"].S;
        int year = int.Parse(attributeList["year"].N);

        Song result = new Song(Guid.Parse(songId), artistName, songTitle, year);
        result.SpotifyUri = spotifyLink;
        result.ArtUri = spotifyArtLink;
        return result;
    }

    private async Task PopulateLocalSongsAsync()
    {
        if (_songs.Count > 0) return;

        try {
            var scanRequest = new ScanRequest
            {
                TableName = _tables.Songs
            };

            var queryResponse = await _client.ScanAsync(scanRequest);
            List<Dictionary<string, AttributeValue>> items = queryResponse.Items;

            ConcurrentDictionary<Guid, Song> result = new();

            Parallel.ForEach(items, song =>
            {
                var tempSong = SongFromResult(song);
                result.TryAdd(tempSong.Id, tempSong);
            });

            _songs = new Dictionary<Guid, Song>(result, result.Comparer);
        } catch (Exception e) {
            _logger.LogCritical(Events.DatabaseFailure, e, "Database query failed. No songs could be fetched.");
        }          
    }
#endregion

    private class Tables
    {
        public Tables(string env)
        {
            if (env == "local" || env == "dev")
            {
                Songs = "1001_songs";
                Users = "1001_users";
                Reviews = "1001_reviews";
                Links = "1001_links";
            } else {
                Songs = "1001_songs_prod";
                Users = "1001_users_prod";
                Reviews = "1001_reviews_prod";
                Links = "1001_links_prod";           
            }
        }

        public string Songs {get; init;}
        public string Users {get; init;}
        public string Reviews {get; init;}
        public string Links {get; init;}
    }

}