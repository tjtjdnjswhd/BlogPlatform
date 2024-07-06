using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogPlatform.Api.Identity.ActionResults
{
    /// <summary>
    /// JWT 토큰 갱신에 대한 <see cref="IActionResult"/>
    /// </summary>
    public class RefreshResult : IActionResult
    {
        private readonly AuthorizeToken _authorizeToken;
        private readonly bool _setCookie;

        public RefreshResult(AuthorizeToken authorizeToken, bool setCookie)
        {
            _setCookie = setCookie;
            _authorizeToken = authorizeToken;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            using var scope = context.HttpContext.RequestServices.CreateScope();
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;

            ILogger<RefreshResult> logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<RefreshResult>();
            logger.LogInformation("Refreshing token begin");
            logger.LogDebug("Access token: {accessToken}. Refresh token: {refreshToken}", _authorizeToken.AccessToken, _authorizeToken.RefreshToken);

            IAuthorizeTokenService authorizeTokenService = scope.ServiceProvider.GetRequiredService<IAuthorizeTokenService>();
            string? oldAccessToken = await authorizeTokenService.GetCachedTokenAsync(_authorizeToken.RefreshToken, cancellationToken);
            if (_authorizeToken.AccessToken != oldAccessToken)
            {
                logger.LogInformation("Refreshing token failed. wrong access token");
                context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            if (!Helper.UserClaimsHelper.TryGetUserId(oldAccessToken, out int userId))
            {
                Debug.Assert(false);
            }

            BlogPlatformDbContext blogPlatformDbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User? user = await blogPlatformDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                logger.LogInformation("Refreshing token failed. User not found");
                await new AuthenticatedUserDataNotFoundResult().ExecuteResultAsync(context);
                return;
            }

            await authorizeTokenService.RemoveTokenAsync(context.HttpContext.Request, context.HttpContext.Response, _authorizeToken.RefreshToken, context.HttpContext.RequestAborted);

            IUserClaimsPrincipalFactory<User> claimsPrincipalFactory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<User>>();
            System.Security.Claims.ClaimsPrincipal claimsPrincipal = await claimsPrincipalFactory.CreateAsync(user);
            JwtAuthenticationProperties authenticationProperties = new(_setCookie);
            await context.HttpContext.SignInAsync(claimsPrincipal, authenticationProperties);
        }
    }
}
