using BlogPlatform.EFCore.Models;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;

namespace BlogPlatform.Api.Identity.ActionResults
{
    /// <summary>
    /// 로그인 성공 시 반환하는 <see cref="IActionResult"/>
    /// </summary>
    public class LoginActionResult : IActionResult
    {
        public User User { get; }

        public string? ReturnUrl { get; }

        public LoginActionResult(User user, string? returnUrl)
        {
            User = user;
            ReturnUrl = returnUrl;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;

            using var serviceScope = context.HttpContext.RequestServices.CreateScope();
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;

            ILogger<LoginActionResult> logger = serviceScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<LoginActionResult>();
            logger.LogInformation("Logging in user {userId}", User.Id);

            IUserClaimsPrincipalFactory<User> claimsPrincipalFactory = serviceScope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<User>>();
            ClaimsPrincipal principal = await claimsPrincipalFactory.CreateAsync(User);
            JwtAuthenticationProperties authenticationProperties = new(ReturnUrl);
            await context.HttpContext.SignInAsync(principal, authenticationProperties);
        }
    }
}
