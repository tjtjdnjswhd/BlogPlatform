
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
        private readonly AuthorizeToken _authorizeToken;
        private readonly bool _setCookie;

        public RefreshResult(AuthorizeToken authorizeToken, bool setCookie)
        {
            _setCookie = setCookie;
            _authorizeToken = authorizeToken;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            using var serviceScope = context.HttpContext.RequestServices.CreateScope();
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;

            ILogger<RefreshResult> logger = serviceScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<RefreshResult>();
            IJwtService jwtService = serviceScope.ServiceProvider.GetRequiredService<IJwtService>();

            AuthorizeToken? newToken = await jwtService.RefreshAsync(_authorizeToken, cancellationToken);
            if (newToken is null)
            {
                logger.LogInformation("Refresh token is expired.");
                context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.HttpContext.Response.WriteAsJsonAsync(new Error("토큰이 만료됬습니다. 다시 로그인 해 주시기 바랍니다."), cancellationToken);
                return;
            }

            await jwtService.SetCacheTokenAsync(newToken, cancellationToken);
            if (_setCookie)
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
