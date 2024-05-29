using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.interfaces;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

using System.Diagnostics;

namespace BlogPlatform.Api.Identity.Filters
{
    public class BasicLoginFilterAttribute : ActionFilterAttribute
    {
        public string InfoArgumentName { get; private set; }

        public BasicLoginFilterAttribute(string infoArgumentName)
        {
            InfoArgumentName = infoArgumentName;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;
            object? info = context.ActionArguments[InfoArgumentName];

            Debug.Assert(info is not null);

            using var scope = context.HttpContext.RequestServices.CreateScope();
            switch (info)
            {
                case BasicSignUpInfo basicSignUpInfo:
                    IVerifyEmailService verifyEmailService = scope.ServiceProvider.GetRequiredService<IVerifyEmailService>();
                    bool isVerified = await verifyEmailService.IsVerifyAsync(basicSignUpInfo.Email, cancellationToken);
                    if (!isVerified)
                    {
                        context.Result = new ForbidResult();
                        await context.HttpContext.Response.WriteAsJsonAsync(new Error("이메일 인증 후 가입해야 합니다."), cancellationToken: CancellationToken.None);
                        return;
                    }
                    break;

                case BasicLoginInfo basicLoginInfo:
                    BlogPlatformDbContext blogPlatformDbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
                    bool isPasswordChangeRequired = await blogPlatformDbContext.BasicAccounts
                        .Where(a => a.AccountId == basicLoginInfo.Id)
                        .Select(a => a.IsPasswordChangeRequired)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (isPasswordChangeRequired)
                    {
                        context.Result = new ForbidResult();
                        await context.HttpContext.Response.WriteAsJsonAsync(new Error("비밀번호 변경 후 로그인해야 합니다."), cancellationToken: CancellationToken.None);
                        return;
                    }
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }

            await next();
        }
    }
}
