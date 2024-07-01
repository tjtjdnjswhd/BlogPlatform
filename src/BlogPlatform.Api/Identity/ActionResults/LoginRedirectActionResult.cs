using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Identity.Services.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.ActionResults
{
    public class LoginRedirectActionResult : IActionResult
    {
        public User? User { get; }

        public string ReturnUrl { get; }

        public LoginRedirectActionResult(User? user, string message, string returnUrl)
        {
            User = user;

            int queryStartIndex = returnUrl.IndexOf('?');
            QueryString queryString;
            switch (queryStartIndex)
            {
                case < 0:
                    queryString = new QueryString();
                    queryString = queryString.Add("message", message);
                    ReturnUrl = returnUrl + queryString.ToString();
                    break;
                default:
                    string queryStrings = returnUrl[(queryStartIndex + 1)..];
                    string escapedQueryStrings = Uri.EscapeDataString(queryStrings);
                    queryString = QueryString.FromUriComponent('?' + escapedQueryStrings);
                    queryString = queryString.Add("message", message);

                    ReturnUrl = returnUrl[..queryStartIndex] + queryString.ToString();
                    break;
            }
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status302Found;
            context.HttpContext.Response.Headers.Location = ReturnUrl;

            if (User is null)
            {
                return;
            }

            using var serviceScope = context.HttpContext.RequestServices.CreateScope();

            ILogger<LoginRedirectActionResult> logger = serviceScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<LoginRedirectActionResult>();
            logger.LogInformation("Logging in user {userId}", User?.Id);

            IJwtService jwtService = serviceScope.ServiceProvider.GetRequiredService<IJwtService>();
            AuthorizeToken token = await jwtService.GenerateTokenAsync(User!);

            logger.LogDebug("Writing token to response cookie: {token}", token);
            jwtService.SetCookieToken(context.HttpContext.Response, token);
        }
    }
}
