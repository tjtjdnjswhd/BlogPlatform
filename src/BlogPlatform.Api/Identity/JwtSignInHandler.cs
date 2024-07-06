using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Shared.Identity.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BlogPlatform.Api.Identity
{
    public class JwtSignInHandler : JwtBearerHandler, IAuthenticationSignInHandler
    {
        public const string AuthenticationScheme = "JwtSignIn";
        private readonly IAuthorizeTokenService _authorizeTokenService;

        public JwtSignInHandler(IAuthorizeTokenService authorizeTokenService, IOptionsMonitor<JwtBearerOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {
            _authorizeTokenService = authorizeTokenService;
        }

        public async Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
        {
            bool setCookie = properties?.GetParameter<bool>(AuthenticationPropertiesParameterKeys.IsSignInCookie) ?? false;
            AuthorizeToken authorizeToken = _authorizeTokenService.GenerateToken(user, setCookie);
            await _authorizeTokenService.WriteAsync(Response, authorizeToken, setCookie);
            await _authorizeTokenService.CacheTokenAsync(authorizeToken, Context.RequestAborted);
        }

        public async Task SignOutAsync(AuthenticationProperties? properties)
        {
            using var scope = Context.RequestServices.CreateScope();
            ILogger<JwtSignInHandler> logger = scope.ServiceProvider.GetRequiredService<ILogger<JwtSignInHandler>>();
            logger.LogInformation("Signing out user");

            await _authorizeTokenService.RemoveTokenAsync(Request, Response, null, Context.RequestAborted);
        }
    }
}
