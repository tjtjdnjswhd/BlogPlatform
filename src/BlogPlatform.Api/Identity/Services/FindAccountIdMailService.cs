using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Services.Interfaces;

using Microsoft.Extensions.Options;

namespace BlogPlatform.Api.Identity.Services
{
    public class FindAccountIdMailService : IFindAccountIdMailService
    {
        private readonly IMailSender _mailSender;
        private readonly FindAccountIdMailOptions _options;
        private readonly ILogger<FindAccountIdMailService> _logger;

        public FindAccountIdMailService(IMailSender mailSender, IOptions<FindAccountIdMailOptions> options, ILogger<FindAccountIdMailService> logger)
        {
            _mailSender = mailSender;
            _options = options.Value;
            _logger = logger;
        }

        public void SendMail(string email, string accountId)
        {
            _logger.LogInformation("Email for account ID {accountId} is sent to {email}", accountId, email);
            _mailSender.Send(_options.From, email, _options.Subject, _options.BodyFactory(accountId), CancellationToken.None);
        }
    }
}
