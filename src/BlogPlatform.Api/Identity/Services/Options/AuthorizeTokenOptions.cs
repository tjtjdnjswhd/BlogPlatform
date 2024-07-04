using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Api.Identity.Services.Options
{
    /// <summary>
    /// JWT 설정
    /// </summary>
    public class AuthorizeTokenOptions
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
