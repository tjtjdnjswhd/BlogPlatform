using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogPlatform.Api.Identity.ActionResults
{
    /// <summary>
    /// JWT 토큰 갱신에 대한 <see cref="IActionResult"/>
    /// </summary>
    public class RefreshResult : IActionResult
    {
        private AuthorizeToken? AuthorizeToken { get; init; }
        private string? ReturnUrl { get; init; }

        public RefreshResult(AuthorizeToken? authorizeToken, string? returnUrl)
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

            IProblemDetailsService problemDetailsService = scope.ServiceProvider.GetRequiredService<IProblemDetailsService>();
            ProblemDetailsFactory problemDetailsFactory = scope.ServiceProvider.GetRequiredService<ProblemDetailsFactory>();

            if (AuthorizeToken is null)
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(context.HttpContext, StatusCodes.Status400BadRequest, detail: "Token required");
                await problemDetailsService.WriteAsync(new ProblemDetailsContext() { HttpContext = context.HttpContext, ProblemDetails = problemDetails });
                return;
            }

            logger.LogDebug("Access token: {accessToken}. Refresh token: {refreshToken}", AuthorizeToken.AccessToken, AuthorizeToken.RefreshToken);

            IAuthorizeTokenService authorizeTokenService = scope.ServiceProvider.GetRequiredService<IAuthorizeTokenService>();
            string? oldAccessToken = await authorizeTokenService.GetCachedTokenAsync(AuthorizeToken.RefreshToken, cancellationToken);
            if (AuthorizeToken.AccessToken != oldAccessToken)
            {
                logger.LogInformation("Refreshing token failed. Expired");
                if (ReturnUrl is null)
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                    ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(context.HttpContext, StatusCodes.Status403Forbidden, detail: "Token expired");
                    await problemDetailsService.WriteAsync(new ProblemDetailsContext() { HttpContext = context.HttpContext, ProblemDetails = problemDetails });
                    return;
                }
                else
                {
                    UriHelper.FromAbsolute(ReturnUrl, out _, out _, out _, query: out var query, out _);
                    string error = "Token expired";
                    query = query.Add("error", error);
                    context.HttpContext.Response.Redirect(ReturnUrl.Split('?')[0] + query);
                }

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
                logger.LogWarning("Refreshing token failed. User not found");

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
