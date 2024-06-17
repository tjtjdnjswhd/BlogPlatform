using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.Interfaces;

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
            ILoggerFactory loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
            ILogger<SignUpEmailVerificationFilterAttribute> logger = loggerFactory.CreateLogger<SignUpEmailVerificationFilterAttribute>();

            CancellationToken cancellationToken = context.HttpContext.RequestAborted;
            BasicSignUpInfo? info = context.ActionArguments[InfoArgumentName] as BasicSignUpInfo;

            Debug.Assert(info is not null);

            using var scope = context.HttpContext.RequestServices.CreateScope();
            IUserEmailService userEmailService = scope.ServiceProvider.GetRequiredService<IUserEmailService>();
            bool isVerified = await userEmailService.IsVerifyAsync(info.Email, cancellationToken);
            if (!isVerified)
            {
                logger.LogInformation("User {email} is not verified for signup", info.Email);
                context.Result = new ForbidResult();
                return;
            }

            logger.LogInformation("User {email} is verified for signup", info.Email);
            await next();
        }
    }
}
