namespace Server1001.Models;

public class UserPrefs {
    public string? ReviewSubmitAction { get; set; }
}

static class AfterSubmit {
    public const string SongPage = "SONG";
    public const string HistoryPage = "HISTORY";
    public const string ReviewPage = "REVIEW";
}