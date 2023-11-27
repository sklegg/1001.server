namespace Server1001.Services;

using System.Net;
using System.Net.Mail;
using Server1001.Shared;

public interface IEmailService
{
    Task<bool> SendMagicTokenEmailAsync(string userAccountEmail, string magicLinkToken);
    Task SendNotFoundEmail(string email);
}

public class EmailService : IEmailService
{
    private ILogger<IEmailService> _logger;
    private SmtpClient client;
    private string baseUrl;

    public EmailService(ILogger<IEmailService> logger, ICustomConfiguration config)
    {
        _logger = logger;
        client = new SmtpClient("email-smtp.us-west-2.amazonaws.com", 587);
        client.Credentials = new NetworkCredential(config.SmtpKeyId, config.SmtpKeySecret);
        client.EnableSsl = true;
        baseUrl = config.ApiBaseUrl;
    }

    public async Task<bool> SendMagicTokenEmailAsync(string userAccountEmail, string magicLinkToken)
    {
        try {
            string emailContent = "Click this magic link to access your 1001 Songs project!\r\n\r\n" + baseUrl + "/user/magic?link=" + magicLinkToken;
            await client.SendMailAsync("wizard@1001songsgenerator.com", userAccountEmail, "Your 1001 Songs Login Link", emailContent);
            return true;
        } catch (Exception e) {
            _logger.LogCritical(Events.EmailError, e, "Email is broken.");
            return false;
        }
    }

    public async Task SendNotFoundEmail(string email)
    {
        try {
            string emailContent = "Hi, friend. I tried to find an account with your email address but I got nothin. Perhaps you need to create a project first?";
            await client.SendMailAsync("wizard@1001songsgenerator.com", email, "Your 1001 Songs Login Link", emailContent);
        } catch (Exception e) {
            _logger.LogCritical(Events.EmailError, e, "Email is broken.");
        }
    }
}