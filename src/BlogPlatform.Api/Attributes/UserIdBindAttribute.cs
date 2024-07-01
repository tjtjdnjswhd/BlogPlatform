using BlogPlatform.Api.ModelBinders;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BlogPlatform.Api.Attributes
{
    public class UserIdBindAttribute : ModelBinderAttribute<UserIdModelBinder>
    {
        public UserIdBindAttribute()
        {
            BindingSource = BindingSource.Special;
        }
    }
}