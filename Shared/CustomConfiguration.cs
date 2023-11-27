namespace Server1001.Shared;

public interface ICustomConfiguration
{
    string SmtpKeyId { get; set; }
    string SmtpKeySecret { get; set; }
    string AuthTokenSecret { get; set; }
    string DatabaseKeyId { get; set; }
    string DatabaseKeySecret { get; set; }
    string DeployEnv { get; set; }
    string ApiBaseUrl { get; set; }
    string WebBaseUrl { get; set; }
}

public class CustomConfiguration : ICustomConfiguration
{
    public CustomConfiguration()
    {        
        SmtpKeyId = Environment.GetEnvironmentVariable("1001__smtpkeyid") ?? "";
        SmtpKeySecret = Environment.GetEnvironmentVariable("1001__smtpkeysecret") ?? "";
        AuthTokenSecret = Environment.GetEnvironmentVariable("1001__authtokensecret") ?? "G467ajPiQvUNUhPK5yPLA9XM7A3AxUBvfoGUTh1C";
        DatabaseKeyId = Environment.GetEnvironmentVariable("1001__databasekeyid") ?? "";
        DatabaseKeySecret = Environment.GetEnvironmentVariable("1001__databasekeysecret") ?? "";
        DeployEnv = Environment.GetEnvironmentVariable("1001__deployenv") ?? "local";
        ApiBaseUrl = Environment.GetEnvironmentVariable("1001__apibaseurl") ?? "https://api-dev.1001songsgenerator.com";
        WebBaseUrl = Environment.GetEnvironmentVariable("1001__webbaseurl") ?? "https://dev.1001songsgenerator.com";
    }

    public string SmtpKeyId { get; set; }
    public string SmtpKeySecret { get; set; }
    public string AuthTokenSecret { get; set; }
    public string DatabaseKeyId { get; set; }
    public string DatabaseKeySecret { get; set; }
    public string DeployEnv { get; set; }
    public string ApiBaseUrl { get; set; }
    public string WebBaseUrl { get; set; }
}
