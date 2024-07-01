namespace BlogPlatform.Shared.Identity.Services.Interfaces
{
    /// <summary>
    /// 유저 이메일 서비스
    /// </summary>
    public interface IUserEmailService
    {
        /// <summary>
        /// 비밀번호 초기화 메일을 전송합니다
        /// </summary>
        /// <param name="email"></param>
        /// <param name="newPassword"></param>
        /// <param name="cancellationToken"></param>
        void SendPasswordResetMail(string email, string newPassword, CancellationToken cancellationToken = default);

        /// <summary>
        /// 아이디 찾기 메일을 전송합니다
        /// </summary>
        /// <param name="email"></param>
        /// <param name="accountId"></param>
        /// <param name="cancellationToken"></param>
        void SendAccountIdMail(string email, string accountId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 이메일 인증 메일을 전송합니다
        /// </summary>
        /// <param name="email"></param>
        /// <param name="uri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"/>
        void SendEmailVerifyMail(string email, string uri, CancellationToken cancellationToken = default);
    }
}
