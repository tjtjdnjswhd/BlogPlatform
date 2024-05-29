using BlogPlatform.Api.Services.interfaces;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.ActionResults
{
    public class AuthenticatedUserDataNotFoundResult : IActionResult
    {
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
