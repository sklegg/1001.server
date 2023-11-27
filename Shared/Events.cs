namespace Server1001.Shared;

public static class Events {
    public static readonly EventId UserNotFound = new EventId(1, "A user could not be found in the database.");
    public static readonly EventId GuidParseError = new EventId(2, "An attempt to parse a string as a Guid failed.");
    public static readonly EventId NoUserContext = new EventId(3, "An invalid or missing user context in a request.");
    public static readonly EventId NoEmailInLogin = new EventId(4, "Login form submitted without an email.");
    public static readonly EventId UserAlreadyExists = new EventId(5, "User attempted to create a user that already exists.");
    public static readonly EventId DatabaseFailure = new EventId(6, "A connection to the database failed.");
    public static readonly EventId SongFetchError = new EventId(7, "An error occured fetching a song from the database.");
    public static readonly EventId EmailError = new EventId(8, "An error occured trying to send an email.");
    public static readonly EventId JwtError = new EventId(9, "An error occured parsing or validating a JWT.");

}