using BlogPlatform.Api.Identity.ModelBinders;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlogPlatform.Api.Identity.Attributes
{
    public class UserIdBindAttribute : ModelBinderAttribute<UserIdModelBinder>
    {
        public UserIdBindAttribute()
        {
            BindingSource = BindingSource.Special;
        }
    }
}
