using BlogPlatform.EFCore;
using BlogPlatform.Shared.Identity.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogPlatform.Api.Identity.Filters
{
    /// <summary>
    /// 로그인 요청 시 비밀번호 변경이 필요한 경우, 요청을 거부하는 필터
    /// </summary>
    public class PasswordChangeRequiredFilterAttribute : ActionFilterAttribute
    {
        public string InfoArgumentName { get; private set; }

        public PasswordChangeRequiredFilterAttribute(string infoArgumentName)
        {
            InfoArgumentName = infoArgumentName;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;
            BasicLoginInfo? info = context.ActionArguments[InfoArgumentName] as BasicLoginInfo;

            Debug.Assert(info is not null);

            using var scope = context.HttpContext.RequestServices.CreateScope();
            BlogPlatformDbContext blogPlatformDbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            bool isPasswordChangeRequired = await blogPlatformDbContext.BasicAccounts
                .Where(a => a.AccountId == info.Id)
                .Select(a => a.IsPasswordChangeRequired)
                .FirstOrDefaultAsync(cancellationToken);

            if (isPasswordChangeRequired)
            {
                IProblemDetailsService problemDetailsService = scope.ServiceProvider.GetRequiredService<IProblemDetailsService>();
                ProblemDetailsFactory problemDetailsFactory = scope.ServiceProvider.GetRequiredService<ProblemDetailsFactory>();
                ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(context.HttpContext, StatusCodes.Status403Forbidden, detail: "Must change password");
                await problemDetailsService.WriteAsync(new ProblemDetailsContext() { HttpContext = context.HttpContext, ProblemDetails = problemDetails });
                return;
            }

            await next();
        }
    }
}
