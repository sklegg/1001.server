namespace Server1001.Models;

public class Summary
{
    public int Count { get; set; }
    public double Average { get; set; }
    public List<Song> Duds { get; set; } = new List<Song>();
    public List<Song> Bangers { get; set; } = new List<Song>();
}