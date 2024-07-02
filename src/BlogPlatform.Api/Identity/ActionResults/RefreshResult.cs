﻿using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;

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

            IAuthorizeTokenService authorizeTokenService = scope.ServiceProvider.GetRequiredService<IAuthorizeTokenService>();
            string? oldAccessToken = await authorizeTokenService.GetCachedTokenAsync(_authorizeToken.RefreshToken, cancellationToken);
            if (_authorizeToken.AccessToken != oldAccessToken)
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            if (!Helper.UserClaimsHelper.TryGetUserId(oldAccessToken, out int userId))
            {
                Debug.Assert(false);
            }

            BlogPlatformDbContext blogPlatformDbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            IUserClaimsPrincipalFactory<User> claimsPrincipalFactory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<User>>();

            User? user = await blogPlatformDbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null)
            {
                await new AuthenticatedUserDataNotFoundResult().ExecuteResultAsync(context);
                return;
            }

            System.Security.Claims.ClaimsPrincipal claimsPrincipal = await claimsPrincipalFactory.CreateAsync(user);
            AuthorizeToken authorizeToken = authorizeTokenService.GenerateToken(claimsPrincipal, _setCookie);
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            await authorizeTokenService.WriteAsync(context.HttpContext.Response, authorizeToken, _setCookie, cancellationToken);
        }
    }
}
