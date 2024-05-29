using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Services.interfaces;
using BlogPlatform.Api.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using System.Diagnostics;

namespace BlogPlatform.Api.Identity.Filters
{
    public class CheckEmailVerifyFilterAttribute : ActionFilterAttribute
    {
        public string InfoArgumentName { get; private set; }

        public CheckEmailVerifyFilterAttribute(string infoArgumentName)
        {
            InfoArgumentName = infoArgumentName;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;
            BasicSignUpInfo? basicSignUpInfo = context.ActionArguments[InfoArgumentName] as BasicSignUpInfo;
            Debug.Assert(basicSignUpInfo is not null);

            using var scope = context.HttpContext.RequestServices.CreateScope();
            IVerifyEmailService verifyEmailService = scope.ServiceProvider.GetRequiredService<IVerifyEmailService>();

            bool isVerified = await verifyEmailService.IsVerifyAsync(basicSignUpInfo.Email, cancellationToken);
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
