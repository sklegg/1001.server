using System.Text.Json.Serialization;

namespace Server1001.Models;

public class Song
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("artist")]
    public string Artist { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; }
    
    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("imageUri")]
    public string? ArtUri { get; set; }

    [JsonPropertyName("spotifyUri")]
    public string? SpotifyUri { get; set;}

    public Song (Guid id, string artist, string title, int year)
    {
        Id = id;
        Artist = artist;
        Title = title;
        Year = year;
    }
}