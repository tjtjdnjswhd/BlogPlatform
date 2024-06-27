using BlogPlatform.Api.Identity.ModelBinders;

using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    /// <summary>
    /// OAuth 인증을 통한 사용자의 로그인 정보 
    /// </summary>
    /// <param name="Provider"> OAuth 제공자 </param>
    /// <param name="NameIdentifier"> OAuth 제공자에서 제공하는 유저 식별자 </param>
    /// 
    [ModelBinder<OAuthInfoModelBinder>]
    public record OAuthLoginInfo([Required(AllowEmptyStrings = false)] string Provider, [Required(AllowEmptyStrings = false)] string NameIdentifier);
}
