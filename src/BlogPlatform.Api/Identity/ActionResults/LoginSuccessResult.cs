using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Services.interfaces;
using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

using System.Security.Claims;

namespace BlogPlatform.Api.Identity.ActionResults
{
    public class LoginSuccessResult : IStatusCodeActionResult
    {
        public int? StatusCode => StatusCodes.Status200OK;

        private readonly User _user;
        private readonly bool _setCookie;

        public LoginSuccessResult(User user, bool setCookie)
        {
            _user = user;
            _setCookie = setCookie;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            ILogger<LoginSuccessResult> _logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger<LoginSuccessResult>();

            IUserClaimsIdentityFactory _userClaimsFactory = context.HttpContext.RequestServices.GetRequiredService<IUserClaimsIdentityFactory>();
            IJwtService _jwtService = context.HttpContext.RequestServices.GetRequiredService<IJwtService>();
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;

            ClaimsIdentity claimsIdentity = await _userClaimsFactory.CreateClaimsIdentityAsync(_user, cancellationToken);
            AuthorizeToken token = _jwtService.GenerateToken(claimsIdentity);
            await _jwtService.SetCacheTokenAsync(token, cancellationToken);
            if (_setCookie)
            {
                _logger.LogDebug("Setting cookie token: {token}", token);
                _jwtService.SetCookieToken(context.HttpContext.Response, token);
            }
            else
            {
                _logger.LogDebug("Writing token to response: {token}", token);
                await context.HttpContext.Response.WriteAsJsonAsync(token, cancellationToken);
            }
        }
    }
}
