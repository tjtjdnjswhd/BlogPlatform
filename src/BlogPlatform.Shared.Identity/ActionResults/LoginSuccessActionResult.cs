using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Identity.Services.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlogPlatform.Shared.Identity.ActionResults
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
