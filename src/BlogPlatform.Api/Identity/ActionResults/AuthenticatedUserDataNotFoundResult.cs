using BlogPlatform.Api.Identity.Services.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BlogPlatform.Api.Identity.ActionResults
{
    /// <summary>
    /// DB에 없는 인증된 사용자가 존재할 때 반환되는 <see cref="IActionResult"/>
    /// </summary>
    public class AuthenticatedUserDataNotFoundResult : IActionResult, IStatusCodeActionResult
    {
        public int? StatusCode => StatusCodes.Status401Unauthorized;

        public Task ExecuteResultAsync(ActionContext context)
        {
            using var scope = context.HttpContext.RequestServices.CreateScope();
            ILoggerFactory loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            ILogger<AuthenticatedUserDataNotFoundResult> logger = loggerFactory.CreateLogger<AuthenticatedUserDataNotFoundResult>();
            logger.LogWarning("Authenticated user data not found. Logging out. user: {user}", context.HttpContext.User.Identities);

            IAuthorizeTokenService authorizeTokenService = scope.ServiceProvider.GetRequiredService<IAuthorizeTokenService>();
            authorizeTokenService.RemoveTokenAsync(context.HttpContext.Request, context.HttpContext.Response, null, default);

            context.HttpContext.Response.StatusCode = StatusCode!.Value;
            return Task.CompletedTask;
        }
    }
}
