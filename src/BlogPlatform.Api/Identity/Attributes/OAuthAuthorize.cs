using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Api.Identity.Attributes
{
    public class OAuthAuthorize : AuthorizeAttribute
    {
        public OAuthAuthorize() : base(PolicyConstants.OAuthPolicy)
        {
        }
    }
}
