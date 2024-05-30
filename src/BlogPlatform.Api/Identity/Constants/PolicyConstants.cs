namespace BlogPlatform.Api.Identity.Constants
{
    /// <summary>
    /// 인증 policy 이름 상수
    /// </summary>
    public static class PolicyConstants
    {
        /// <summary>
        /// OAuth 인증 policy 이름
        /// </summary>
        public const string OAuthPolicy = "oauth";

        /// <summary>
        /// User 인증 policy 이름
        /// </summary>
        public const string UserPolicy = "user";

        /// <summary>
        /// Admin 인증 policy 이름
        /// </summary>
        public const string AdminPolicy = "admin";
    }
}
