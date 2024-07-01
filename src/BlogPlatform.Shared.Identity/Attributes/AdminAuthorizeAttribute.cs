using BlogPlatform.Shared.Identity.Constants;

using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Shared.Identity.Attributes
{
    /// <summary>
    /// Admin policy를 적용하는 AuthorizeAttribute
    /// </summary>
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        public AdminAuthorizeAttribute() : base(PolicyConstants.AdminPolicy) { }
    }
}
