using BlogPlatform.Api.Identity.Constants;

using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Api.Identity.Attributes
{
    public class OAuthAuthorizeAttribute : AuthorizeAttribute
    {
        public OAuthAuthorizeAttribute() : base(PolicyConstants.OAuthPolicy)
        {
        }
    }
}
