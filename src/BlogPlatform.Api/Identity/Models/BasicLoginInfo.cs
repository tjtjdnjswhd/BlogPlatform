﻿using BlogPlatform.Api.Identity.Attributes;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Models
{
    /// <summary>
    /// Id/pw로 로그인 시 사용되는 정보
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="Password"></param>
    public record BasicLoginInfo([Required(AllowEmptyStrings = false), AccountIdValidate] string Id, [Required(AllowEmptyStrings = false), AccountPasswordValidate] string Password);
}
