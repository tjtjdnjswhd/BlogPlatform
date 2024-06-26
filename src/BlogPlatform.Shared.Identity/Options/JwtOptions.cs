﻿using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace BlogPlatform.Shared.Identity.Options
{
    /// <summary>
    /// JWT 설정
    /// </summary>
    public class JwtOptions
    {
        /// <summary>
        /// 비밀 키
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string SecretKey { get; set; }

        /// <summary>
        /// 해시 알고리즘
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string Algorithm { get; set; }

        /// <summary>
        /// <see cref="ClaimsIdentity"/> 생성 시 설정할 인증 타입. <see cref="ClaimsIdentity.AuthenticationType"/>
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string AuthenticationType { get; set; }

        /// <summary>
        /// 발급자
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string Issuer { get; set; }

        /// <summary>
        /// 대상자
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string Audience { get; set; }

        /// <summary>
        /// 쿠키, 캐시에 사용될 엑세스 토큰 이름
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string AccessTokenName { get; set; }

        /// <summary>
        /// 쿠키, 캐시에 사용될 리프레시 토큰 이름
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public required string RefreshTokenName { get; set; }

        /// <summary>
        /// 엑세스 토큰 만료 시간
        /// </summary>
        [Required]
        public required TimeSpan AccessTokenExpiration { get; set; }

        /// <summary>
        /// 리프레시 토큰 만료 시간
        /// </summary>
        [Required]
        public required TimeSpan RefreshTokenExpiration { get; set; }
    }
}
