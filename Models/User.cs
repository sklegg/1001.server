namespace Server1001.Models;

public class User
{
    public User (Guid id, string email)
    {
        Id = id;
        Email = email.Trim();
    }

    public Guid Id { get; set; }
    public string? Email { get; set; }
    public Guid? CurrentSongId { get; set; }
    public DateTime? Created { get; set; }
    public bool ActiveFlag { get; set; }
    public UserPrefs Preferences { get; set; }
}