
using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Models;
using BlogPlatform.Api.Services.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.ActionResults
{
    /// <summary>
    /// JWT 토큰 갱신에 대한 <see cref="IActionResult"/>
    /// </summary>
    public class RefreshResult : IActionResult
    {
        public async Task ExecuteResultAsync(ActionContext context)
        {
            using var serviceScope = context.HttpContext.RequestServices.CreateScope();
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;

            ILogger<RefreshResult> logger = serviceScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<RefreshResult>();
            IJwtService jwtService = serviceScope.ServiceProvider.GetRequiredService<IJwtService>();

            bool isCookieToken = true;

            AuthorizeToken? oldToken = jwtService.GetCookieToken(context.HttpContext.Request);
            if (oldToken is null)
            {
                logger.LogInformation("No token found in request cookie.");
                oldToken = await jwtService.GetBodyTokenAsync(context.HttpContext.Request, cancellationToken);
                isCookieToken = false;
                if (oldToken is null)
                {
                    logger.LogInformation("No token found in request body.");
                    context.HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                    await context.HttpContext.Response.WriteAsJsonAsync(new Error("토큰이 필요합니다."));
                    return;
                }
            }

            AuthorizeToken? newToken = await jwtService.RefreshAsync(oldToken, cancellationToken);
            if (newToken is null)
            {
                logger.LogInformation("Refresh token is expired.");
                context.HttpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                await context.HttpContext.Response.WriteAsJsonAsync(new Error("토큰이 만료됬습니다. 다시 로그인 해 주시기 바랍니다."), cancellationToken);
                return;
            }

            await jwtService.SetCacheTokenAsync(newToken, cancellationToken);
            if (isCookieToken)
            {
                logger.LogDebug("Setting cookie token: {token}", newToken);
                jwtService.SetCookieToken(context.HttpContext.Response, newToken);
            }
            else
            {
                logger.LogDebug("Writing token to response: {token}", newToken);
                await jwtService.SetBodyTokenAsync(context.HttpContext.Response, newToken, cancellationToken);
            }
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        }
    }
}
