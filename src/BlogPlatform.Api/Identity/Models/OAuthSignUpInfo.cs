﻿using BlogPlatform.Api.Identity.Attributes;
using BlogPlatform.Api.Identity.ModelBinders;

using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    /// <summary>
    /// OAuth 인증을 통한 사용자의 가입 정보
    /// </summary>
    /// <param name="Name"> 가입할 사용자의 이름 </param>
    /// <param name="Email"> 가입할 사용자의 이메일 </param>
    [ModelBinder<OAuthInfoModelBinder>]
    public record OAuthSignUpInfo([UserNameValidate] string Name, [EmailAddress] string Email, string Provider, string NameIdentifier) : OAuthLoginInfo(Provider, NameIdentifier);
}
