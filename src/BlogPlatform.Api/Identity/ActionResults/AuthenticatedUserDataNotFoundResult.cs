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

        public async Task ExecuteResultAsync(ActionContext context)
        {
            using var scope = context.HttpContext.RequestServices.CreateScope();
            ILoggerFactory loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            ILogger<AuthenticatedUserDataNotFoundResult> logger = loggerFactory.CreateLogger<AuthenticatedUserDataNotFoundResult>();
            logger.LogWarning("Authenticated user data not found. Logging out. user: {user}", context.HttpContext.User.Identities);

            ProblemDetailsFactory problemDetailsFactory = scope.ServiceProvider.GetRequiredService<ProblemDetailsFactory>();
            IProblemDetailsService problemDetailsService = scope.ServiceProvider.GetRequiredService<IProblemDetailsService>();
            context.HttpContext.Response.StatusCode = StatusCode!.Value;
            await problemDetailsService.WriteAsync(new ProblemDetailsContext()
            {
                HttpContext = context.HttpContext,
                ProblemDetails = problemDetailsFactory.CreateProblemDetails(context.HttpContext, StatusCode!.Value, detail: "User not exist")
            });

            IAuthorizeTokenService authorizeTokenService = scope.ServiceProvider.GetRequiredService<IAuthorizeTokenService>();
            await authorizeTokenService.RemoveTokenAsync(context.HttpContext.Request, context.HttpContext.Response, null, default);
        }
    }
}
