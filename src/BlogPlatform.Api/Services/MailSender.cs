using BlogPlatform.Api.Options;
using BlogPlatform.Api.Services.Interfaces;

using MailKit.Net.Smtp;

using Microsoft.Extensions.Options;

using MimeKit;

namespace BlogPlatform.Api.Services
{
    public class MailSender : IMailSender
    {
        private readonly MailSenderOptions _mailOptions;
        private readonly ILogger<MailSender> _logger;

        public MailSender(IOptions<MailSenderOptions> mailOptions, ILogger<MailSender> logger)
        {
            _mailOptions = mailOptions.Value;
            _logger = logger;
        }

        public void Send(string from, string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                """
                Sending email.
                from: {from}
                to: {to}
                subject: {subject}
                body:
                {body}
                """,
                from, to, subject, body);

            MimeMessage message = new();
            message.From.Add(new MailboxAddress("no-reply", from));
            message.To.Add(new MailboxAddress("user", to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            _logger.LogDebug("MimeMessage: {message}", message);

            using SmtpClient smtpClient = new();
            smtpClient.Connect(_mailOptions.Host, _mailOptions.Port, true, cancellationToken);
            smtpClient.Authenticate(_mailOptions.Username, _mailOptions.Password, cancellationToken);
            string responseText = smtpClient.Send(message, cancellationToken);
            smtpClient.Disconnect(true, cancellationToken);

            _logger.LogDebug("Email sent. response: {responseText}", responseText);
        }
    }
}
