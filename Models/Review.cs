using System.Text.Json.Serialization;

namespace Server1001.Models;

public class Review
{
    [JsonPropertyName("reviewId")]
    public Guid? ReviewId { get; set; }

    [JsonPropertyName("userId")]
    public Guid? ReviewerId { get; set;}
    
    [JsonPropertyName("songId")]
    public Guid? SongId { get; set;}
    
    [JsonPropertyName("stars")]
    public int StarRating { get; set;}
    
    [JsonPropertyName("comment")]
    public string? Comment { get; set;}
    
    [JsonPropertyName("dateTime")]
    public DateTime PostedDateTime { get; set;}
}