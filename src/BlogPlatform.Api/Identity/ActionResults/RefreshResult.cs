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
        private readonly string? _returnUrl;

        public RefreshResult(AuthorizeToken authorizeToken, string? returnUrl)
        {
            _authorizeToken = authorizeToken;
            _returnUrl = returnUrl;
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

            await authorizeTokenService.RemoveCachedTokenAsync(_authorizeToken.RefreshToken, cancellationToken);

            IUserClaimsPrincipalFactory<User> claimsPrincipalFactory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<User>>();
            System.Security.Claims.ClaimsPrincipal claimsPrincipal = await claimsPrincipalFactory.CreateAsync(user);
            JwtAuthenticationProperties authenticationProperties = new(_returnUrl);
            await context.HttpContext.SignInAsync(claimsPrincipal, authenticationProperties);
        }
    }
}
