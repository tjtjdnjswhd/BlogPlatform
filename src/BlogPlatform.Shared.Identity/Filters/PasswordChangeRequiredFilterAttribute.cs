using BlogPlatform.EFCore;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using System.Diagnostics;

namespace BlogPlatform.Shared.Identity.Filters
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
                context.Result = new ForbidResult();
                await context.HttpContext.Response.WriteAsJsonAsync(new Error("비밀번호 변경 후 로그인해야 합니다."), cancellationToken: CancellationToken.None);
                return;
            }

            await next();
        }
    }
}
