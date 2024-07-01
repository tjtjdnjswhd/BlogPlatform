using BlogPlatform.Shared.Identity.Constants;

using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Shared.Identity.Attributes
{
    /// <summary>
    /// OAuth policy를 적용하는 AuthorizeAttribute
    /// </summary>
    public class OAuthAuthorizeAttribute : AuthorizeAttribute
    {
        public OAuthAuthorizeAttribute() : base(PolicyConstants.OAuthPolicy) { }
    }
}
