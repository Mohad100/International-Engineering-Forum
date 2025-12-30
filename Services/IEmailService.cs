namespace Fourm.Services;

/// <summary>
/// Interface for email service
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a welcome email to a newly registered user
    /// </summary>
    Task<bool> SendWelcomeEmailAsync(string toEmail, string username, string major);

    /// <summary>
    /// Sends a generic email
    /// </summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string body);
}
