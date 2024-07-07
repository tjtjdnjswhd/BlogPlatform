using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Identity.Services.Options;

using Microsoft.Extensions.Options;

namespace BlogPlatform.Api.Identity.Services
{
    public class ReturnUrlWhitelistService : IReturnUrlWhitelistService
    {
        private readonly ReturnUrlWhitelistOptions _options;

        public ReturnUrlWhitelistService(IOptions<ReturnUrlWhitelistOptions> options)
        {
            _options = options.Value;
        }

        public bool IsWhitelisted(string returnUrl)
        {
            if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out Uri? uri))
            {
                return false;
            }

            string authority = uri.GetLeftPart(UriPartial.Authority);
            return _options.Contains(authority);
        }
    }
}
