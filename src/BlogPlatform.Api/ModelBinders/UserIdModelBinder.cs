using BlogPlatform.Shared.Identity.Services.Interfaces;

using Microsoft.AspNetCore.Mvc.ModelBinding;

using System.Diagnostics;

namespace BlogPlatform.Api.ModelBinders
{
    public class UserIdModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(int))
            {
                bindingContext.Result = ModelBindingResult.Failed();
                return Task.CompletedTask;
            }

            using var scope = bindingContext.HttpContext.RequestServices.CreateScope();
            IIdentityService identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
            if (identityService.TryGetUserId(bindingContext.HttpContext.User, out int userId))
            {
                bindingContext.Result = ModelBindingResult.Success(userId);
                return Task.CompletedTask;
            }

            Debug.Assert(false);
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }
    }
}
