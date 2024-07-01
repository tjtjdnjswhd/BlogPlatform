using BlogPlatform.Shared.Identity.Constants;

using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Shared.Identity.Attributes
{
    /// <summary>
    /// User Policy를 적용하는 AuthorizeAttribute
    /// </summary>
    public class UserAuthorizeAttribute : AuthorizeAttribute
    {
        public UserAuthorizeAttribute() : base(PolicyConstants.UserPolicy) { }
    }
}
