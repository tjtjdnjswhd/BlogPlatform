using BlogPlatform.Api.Identity.Constants;

using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Api.Identity.Attributes
{
    /// <summary>
    /// User Policy를 적용하는 AuthorizeAttribute
    /// </summary>
    public class UserAuthorizeAttribute : AuthorizeAttribute
    {
        public UserAuthorizeAttribute() : base(PolicyConstants.UserRolePolicy) { }
    }
}
