using BlogPlatform.Api.Identity.ModelBinders;

using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.Attributes
{
    public class UserIdBindAttribute : ModelBinderAttribute<UserIdModelBinder>
    {
        public UserIdBindAttribute() { }
    }
}
