using BlogPlatform.Api.Constants;

using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.Attributes
{
    /// <summary>
    /// JWT 토큰을 쿠키에 설정하는 헤더를 가져오는 Attribute
    /// </summary>
    public class TokenSetCookieAttribute : FromHeaderAttribute
    {
        public TokenSetCookieAttribute()
        {
            Name = HeaderNameConstants.AuthorizeTokenSetCookie;
        }
    }
}
