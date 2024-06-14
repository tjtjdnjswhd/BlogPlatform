using BlogPlatform.Api.Services.Interfaces;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.ActionResults
{
    /// <summary>
    /// DB에 없는 인증된 사용자가 존재할 때 반환되는 <see cref="IActionResult"/>
    /// </summary>
    public class AuthenticatedUserDataNotFoundResult : IActionResult, IStatusCodeHttpResult
    {
        public int? StatusCode => StatusCodes.Status401Unauthorized;

        public async Task ExecuteResultAsync(ActionContext context)
        {
            using var scope = context.HttpContext.RequestServices.CreateScope();
            IJwtService jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
            using var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            ILogger<AuthenticatedUserDataNotFoundResult> logger = loggerFactory.CreateLogger<AuthenticatedUserDataNotFoundResult>();

            logger.LogWarning("Authenticated user data not found. Logging out. user: {user}", context.HttpContext.User.Identities);

            jwtService.RemoveCookieToken(context.HttpContext.Request, context.HttpContext.Response);
            await context.HttpContext.SignOutAsync();
        }
    }
}
