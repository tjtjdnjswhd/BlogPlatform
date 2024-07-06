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
            if (properties is not JwtAuthenticationProperties jwtProperties)
            {
                throw new ArgumentException("The properties must be of type JwtAuthenticationProperties", nameof(properties));
            }

            bool setCookie = jwtProperties.IsSignInCookie;
            AuthorizeToken authorizeToken = _authorizeTokenService.GenerateToken(user, setCookie);

            if (setCookie)
            {
                _authorizeTokenService.SetCookieToken(Response, authorizeToken);
            }
            else
            {
                await _authorizeTokenService.WriteBodyTokenAsync(Response, authorizeToken, Context.RequestAborted);
            }

            await _authorizeTokenService.CacheTokenAsync(authorizeToken, Context.RequestAborted);
            if (properties?.RedirectUri != null)
            {
                Response.Redirect(properties.RedirectUri);
            }
        }

        public async Task SignOutAsync(AuthenticationProperties? properties)
        {
            using var scope = Context.RequestServices.CreateScope();
            ILogger<JwtSignInHandler> logger = scope.ServiceProvider.GetRequiredService<ILogger<JwtSignInHandler>>();
            logger.LogInformation("Signing out user");

            await _authorizeTokenService.RemoveTokenAsync(Request, Response, null, Context.RequestAborted);
            _authorizeTokenService.ExpireCookieToken(Response);
            if (properties?.RedirectUri != null)
            {
                Response.Redirect(properties.RedirectUri);
            }
        }
    }
}
