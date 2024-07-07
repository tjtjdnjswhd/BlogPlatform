namespace BlogPlatform.Api.BrowserTests.Options
{
    public class ApiUrls
    {
        public required string BaseAddress { get; set; }

        public required IdentityUrls Identity { get; set; }

        public class IdentityUrls
        {
            public required string UserInfo { get; set; }

            public required string BasicLogin { get; set; }

            public required string BasicSignUp { get; set; }

            public required string SendVerifyEmail { get; set; }

            public required string OAuthLogin { get; set; }

            public required string OAuthSignUp { get; set; }

            public required string OAuthAdd { get; set; }

            public required string OAuthRemove { get; set; }

            public required string Logout { get; set; }

            public required string Refresh { get; set; }

            public required string ChangePassword { get; set; }

            public required string ResetPassword { get; set; }

            public required string ChangeName { get; set; }

            public required string ChangeEmail { get; set; }

            public required string FindId { get; set; }

            public required string Withdraw { get; set; }

            public required string CancelWithdraw { get; set; }
        }
    }
}
