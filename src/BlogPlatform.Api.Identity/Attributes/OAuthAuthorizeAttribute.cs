using BlogPlatform.Api.Identity.Constants;

using Microsoft.AspNetCore.Authorization;

namespace BlogPlatform.Api.Identity.Attributes
{
    /// <summary>
    /// OAuth policy를 적용하는 AuthorizeAttribute
    /// </summary>
    public class OAuthAuthorizeAttribute : AuthorizeAttribute
    {
        public OAuthAuthorizeAttribute() : base(PolicyConstants.OAuthPolicy) { }
    }
}
