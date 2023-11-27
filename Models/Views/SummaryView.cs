using System.Text.Json.Serialization;

namespace Server1001.Models.Views;

public class SummaryView
{
    public SummaryView(int count, double average, List<Song> fives, List<Song> ones)
    {
        ReviewCount = count;
        ReviewAverage = average;
        
        Bangers = new List<SongSummaryView>();
        foreach (Song s in fives)
        {
            Bangers.Add(new SongSummaryView(s));
        }

        Duds = new List<SongSummaryView>();
        foreach (Song s in ones)
        {
            Duds.Add(new SongSummaryView(s));
        }
    }

    public SummaryView(Summary s) : this(s.Count, s.Average, s.Bangers, s.Duds) { }

    [JsonPropertyName("reviewCount")]
    public int ReviewCount { get; set; }
    
    [JsonPropertyName("reviewAverage")]
    public double ReviewAverage { get; set;}

    [JsonPropertyName("bangers")]
    public List<SongSummaryView> Bangers { get; set; }

    [JsonPropertyName("duds")]
    public List<SongSummaryView> Duds { get; set; }
}

public class SongSummaryView
{
    public SongSummaryView(Song s)
    {
        Artist = s.Artist;
        Title = s.Title;
        Year = s.Year;
    }

    [JsonPropertyName("artist")]
    public string Artist { get; init; }
    
    [JsonPropertyName("title")]
    public string Title { get; init; }
    
    [JsonPropertyName("year")]
    public int Year { get; init; }    
}