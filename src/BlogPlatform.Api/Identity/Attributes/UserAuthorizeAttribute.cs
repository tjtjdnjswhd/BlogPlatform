using BlogPlatform.Api.Identity.Constants;

using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Api.Identity.Attributes
{
    public class UserAuthorizeAttribute : AuthorizeAttribute
    {
        public UserAuthorizeAttribute() : base(PolicyConstants.UserPolicy) { }
    }
}
