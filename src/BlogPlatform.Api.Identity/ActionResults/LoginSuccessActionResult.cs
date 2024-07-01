using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.ActionResults
{
    /// <summary>
    /// 로그인 성공 시 반환하는 <see cref="IActionResult"/>
    /// </summary>
    public class LoginSuccessActionResult : IActionResult
    {
        public User User { get; }

        public LoginSuccessActionResult(User user)
        {
            User = user;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            using var serviceScope = context.HttpContext.RequestServices.CreateScope();
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;

            ILogger<LoginSuccessActionResult> logger = serviceScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<LoginSuccessActionResult>();

            logger.LogInformation("Logging in user {userId}", User.Id);

            BlogPlatformDbContext blogPlatformDbContext = serviceScope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            IJwtService jwtService = serviceScope.ServiceProvider.GetRequiredService<IJwtService>();

            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
            AuthorizeToken token = await jwtService.GenerateTokenAsync(User, cancellationToken);
            await jwtService.SetCacheTokenAsync(token, cancellationToken);
            logger.LogDebug("Writing token to response body: {token}", token);
            await jwtService.SetBodyTokenAsync(context.HttpContext.Response, token, cancellationToken);
        }
    }
}
