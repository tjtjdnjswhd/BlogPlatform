using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Identity.Services.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlogPlatform.Shared.Identity.ActionResults
{
    /// <summary>
    /// 로그아웃 요청에 대한 <see cref="IActionResult"/>
    /// </summary>
    public class LogoutResult : IActionResult
    {
        public async Task ExecuteResultAsync(ActionContext context)
        {
            ILogger<LogoutResult> logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger<LogoutResult>();
            IJwtService jwtService = context.HttpContext.RequestServices.GetRequiredService<IJwtService>();
            AuthorizeToken? token = jwtService.RemoveCookieToken(context.HttpContext.Request, context.HttpContext.Response);

            switch (token)
            {
                case null:
                    logger.LogInformation("No token found in cookie.");
                    context.HttpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                    break;
                default:
                    logger.LogInformation("Removing cached token: {token}", token);
                    await jwtService.RemoveCachedTokenAsync(token.RefreshToken, CancellationToken.None);
                    context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
                    break;
            }
        }
    }
}
