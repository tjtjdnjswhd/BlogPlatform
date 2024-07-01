using BlogPlatform.Shared.Identity.ModelBinders;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlogPlatform.Shared.Identity.Attributes
{
    public class UserIdBindAttribute : ModelBinderAttribute<UserIdModelBinder>
    {
        public UserIdBindAttribute()
        {
            BindingSource = BindingSource.Special;
        }
    }
}
