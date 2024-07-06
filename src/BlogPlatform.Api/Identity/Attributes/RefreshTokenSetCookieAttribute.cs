using BlogPlatform.Api.Identity.Constants;

using Microsoft.AspNetCore.Mvc;

namespace BlogPlatform.Api.Identity.Attributes
{
    /// <summary>
    /// 토큰 갱신시 쿠키 저장 여부 헤더를 받아올 때 사용하는 <see cref="FromHeaderAttribute"/>
    /// </summary>
    public class RefreshTokenSetCookieAttribute : FromHeaderAttribute
    {
        public RefreshTokenSetCookieAttribute()
        {
            Name = HeaderNameConstants.AuthorizeTokenSetCookie;
        }
    }
}
