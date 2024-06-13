using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using System.Diagnostics;

namespace BlogPlatform.Api.Identity.Filters
{
    /// <summary>
    /// 회원가입 시 이메일 인증이 필요한 경우, 요청을 거부하는 필터
    /// </summary>
    public class SignUpEmailVerificationFilterAttribute : ActionFilterAttribute
    {
        public string InfoArgumentName { get; private set; }

        public SignUpEmailVerificationFilterAttribute(string infoArgumentName)
        {
            InfoArgumentName = infoArgumentName;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;
            BasicSignUpInfo? info = context.ActionArguments[InfoArgumentName] as BasicSignUpInfo;

            Debug.Assert(info is not null);

            using var scope = context.HttpContext.RequestServices.CreateScope();
            IUserEmailService userEmailService = scope.ServiceProvider.GetRequiredService<IUserEmailService>();
            bool isVerified = await userEmailService.IsVerifyAsync(info.Email, cancellationToken);
            if (!isVerified)
            {
                context.Result = new ForbidResult();
                await context.HttpContext.Response.WriteAsJsonAsync(new Error("이메일 인증 후 가입해야 합니다."), cancellationToken: CancellationToken.None);
                return;
            }

            await next();
        }
    }
}
