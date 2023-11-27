using System.Text.Json.Serialization;

namespace Server1001.Models.Views;

public class HistoryView
{
    public HistoryView(Review r, Song s)
    {
        Review = r;
        ReviewedSong = s;
    }

    [JsonPropertyName("review")]
    public Review Review { get; set; }
    
    [JsonPropertyName("reviewedSong")]
    public Song ReviewedSong { get; set;}
    
    [JsonPropertyName("count")]
    public int ReviewCount { get; set; }

    [JsonPropertyName("average")]
    public double ReviewAverage { get; set; }    
}