﻿namespace BlogPlatform.Api.Identity.Constants
{
    /// <summary>
    /// 인증 policy 이름 상수
    /// </summary>
    public static class PolicyConstants
    {
        /// <summary>
        /// OAuth 인증 policy 이름
        /// </summary>
        public const string OAuthPolicy = "Oauth";

        /// <summary>
        /// User 인증 policy 이름
        /// </summary>
        public const string UserRolePolicy = "User";

        /// <summary>
        /// Admin 인증 policy 이름
        /// </summary>
        public const string AdminRolePolicy = "Admin";
    }
}
