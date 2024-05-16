using BlogPlatform.Api.Identity.Constants;

using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.Attributes
{
    public class TokenSetCookieAttribute : FromHeaderAttribute
    {
        public TokenSetCookieAttribute()
        {
            Name = HeaderNameConstants.AuthorizeTokenSetCookie;
        }
    }
}
