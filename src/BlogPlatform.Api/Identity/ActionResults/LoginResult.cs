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
    public class LoginResult : IActionResult
    {
        public User User { get; }

        public bool SetCookie { get; }

        public LoginResult(User user, bool setCookie)
        {
            User = user;
            SetCookie = setCookie;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            using var serviceScope = context.HttpContext.RequestServices.CreateScope();
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;

            ILogger<LoginResult> logger = serviceScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<LoginResult>();
            logger.LogInformation("Logging in user {userId}", User.Id);

            BlogPlatformDbContext blogPlatformDbContext = serviceScope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            IJwtService jwtService = serviceScope.ServiceProvider.GetRequiredService<IJwtService>();

            AuthorizeToken token = await jwtService.GenerateTokenAsync(User, cancellationToken);
            await jwtService.SetCacheTokenAsync(token, cancellationToken);

            if (SetCookie)
            {
                logger.LogDebug("Setting cookie token: {token}", token);
                jwtService.SetCookieToken(context.HttpContext.Response, token);
            }
            else
            {
                logger.LogDebug("Writing token to response: {token}", token);
                await jwtService.SetBodyTokenAsync(context.HttpContext.Response, token, cancellationToken);
            }
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        }
    }
}
