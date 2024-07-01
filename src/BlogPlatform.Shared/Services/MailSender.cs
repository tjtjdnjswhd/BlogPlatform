using BlogPlatform.Shared.Options;
using BlogPlatform.Shared.Services.Interfaces;

using MailKit.Net.Smtp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MimeKit;

namespace BlogPlatform.Shared.Services
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

        /// <inheritdoc/>
        public void Send(MailSendContext context, CancellationToken cancellationToken = default)
        {
            MailboxAddress fromAddress = new(_mailOptions.SenderName, $"{context.FromId ?? "noreply"}@{_mailOptions.Domain}");
            MailboxAddress toAddress = new(context.ReceiverName, context.To);
            MimeEntity bodyHtml = new TextPart("html") { Text = context.Body };

            _logger.LogInformation(
                """
                Sending email.
                from: {from}
                to: {to}
                subject: {subject}
                body:
                {body}
                """,
                fromAddress, toAddress, context.Subject, bodyHtml);

            MimeMessage message = new([fromAddress], [toAddress], context.Subject, bodyHtml);

            using SmtpClient smtpClient = new();
            smtpClient.Connect(_mailOptions.Host, _mailOptions.Port, true, cancellationToken);
            smtpClient.Authenticate(_mailOptions.Username, _mailOptions.Password, cancellationToken);
            string responseText = smtpClient.Send(message, cancellationToken);
            smtpClient.Disconnect(true, CancellationToken.None);

            _logger.LogInformation("Email sent. response: {responseText}", responseText);
        }
    }
}
