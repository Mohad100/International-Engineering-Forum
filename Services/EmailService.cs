using System.Net;
using System.Net.Mail;

namespace Fourm.Services;

/// <summary>
/// Service for sending emails
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Sends a welcome email to a newly registered user
    /// </summary>
    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string username, string major)
    {
        var subject = "Welcome to International Engineering Forum! 🎉";
        
        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #f4f4f4; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: white; border-radius: 15px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 20px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .content {{ padding: 40px 30px; }}
        .content h2 {{ color: #667eea; margin-top: 0; }}
        .info-box {{ background: #f8f9ff; border-left: 4px solid #667eea; padding: 15px; margin: 20px 0; border-radius: 5px; }}
        .button {{ display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 12px 30px; text-decoration: none; border-radius: 25px; margin: 20px 0; }}
        .footer {{ background: #f8f9ff; padding: 20px; text-align: center; color: #666; font-size: 14px; }}
        .emoji {{ font-size: 24px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='emoji'>⚙️</div>
            <h1>Welcome to International Engineering Forum!</h1>
        </div>
        <div class='content'>
            <h2>Hello {username}! 👋</h2>
            <p>Congratulations on joining the <strong>International Engineering Forum</strong>! We're thrilled to have you as part of our global engineering community.</p>
            
            <div class='info-box'>
                <strong>📋 Your Registration Details:</strong><br>
                <strong>Username:</strong> {username}<br>
                <strong>Email:</strong> {toEmail}<br>
                <strong>Major:</strong> {major}<br>
                <strong>Registered on:</strong> {DateTime.UtcNow:MMMM dd, yyyy}
            </div>

            <h3>🚀 Get Started:</h3>
            <ul>
                <li><strong>Explore Categories:</strong> Browse through various engineering disciplines</li>
                <li><strong>Start Discussions:</strong> Share your knowledge and ask questions</li>
                <li><strong>Connect with Peers:</strong> Network with engineers worldwide</li>
                <li><strong>Stay Professional:</strong> Follow our community guidelines</li>
            </ul>

            <div style='text-align: center;'>
                <a href='#' class='button'>Visit Forum Now 🎯</a>
            </div>

            <p style='margin-top: 30px;'>If you have any questions or need assistance, feel free to reach out to our community moderators.</p>
            
            <p><strong>Happy Engineering! 🔧</strong></p>
        </div>
        <div class='footer'>
            <p>This is an automated email. Please do not reply directly to this message.</p>
            <p>&copy; 2025 International Engineering Forum. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        return await SendEmailAsync(toEmail, subject, body);
    }

    /// <summary>
    /// Sends a generic email
    /// </summary>
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            // Get email settings from configuration
            var smtpHost = _configuration["EmailSettings:SmtpHost"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"];
            var fromName = _configuration["EmailSettings:FromName"];

            // If email settings are not configured, log and return true (for development)
            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername))
            {
                _logger.LogWarning("Email settings not configured. Email not sent to {Email}", toEmail);
                _logger.LogInformation("Would have sent email to {Email} with subject: {Subject}", toEmail, subject);
                return true; // Return true to not block registration
            }

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUsername, smtpPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail ?? smtpUsername ?? "noreply@forum.com", fromName ?? "International Engineering Forum"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
            return false; // Don't block registration if email fails
        }
    }
}
