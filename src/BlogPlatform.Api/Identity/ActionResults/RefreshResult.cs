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
        private AuthorizeToken AuthorizeToken { get; init; }
        private string? ReturnUrl { get; init; }

        public RefreshResult(AuthorizeToken authorizeToken, string? returnUrl)
        {
            AuthorizeToken = authorizeToken;
            ReturnUrl = returnUrl;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            using var scope = context.HttpContext.RequestServices.CreateScope();
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;

            ILogger<RefreshResult> logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<RefreshResult>();
            logger.LogInformation("Refreshing token begin");
            logger.LogDebug("Access token: {accessToken}. Refresh token: {refreshToken}", AuthorizeToken.AccessToken, AuthorizeToken.RefreshToken);

            IAuthorizeTokenService authorizeTokenService = scope.ServiceProvider.GetRequiredService<IAuthorizeTokenService>();
            string? oldAccessToken = await authorizeTokenService.GetCachedTokenAsync(AuthorizeToken.RefreshToken, cancellationToken);
            if (AuthorizeToken.AccessToken != oldAccessToken)
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

            await authorizeTokenService.RemoveCachedTokenAsync(AuthorizeToken.RefreshToken, cancellationToken);

            IUserClaimsPrincipalFactory<User> claimsPrincipalFactory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<User>>();
            System.Security.Claims.ClaimsPrincipal claimsPrincipal = await claimsPrincipalFactory.CreateAsync(user);
            JwtAuthenticationProperties authenticationProperties = new(ReturnUrl);
            await context.HttpContext.SignInAsync(claimsPrincipal, authenticationProperties);
        }
    }
}
