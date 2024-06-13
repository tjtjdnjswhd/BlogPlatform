namespace BlogPlatform.Api.Identity.Services.Interfaces
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
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"/>
        Task SendEmailVerificationAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// 인증 코드를 확인합니다
        /// </summary>
        /// <param name="code"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 인증 성공 시 이메일을 반환합니다.
        /// 실패 시, null을 반환합니다.
        /// </returns>
        /// <exception cref="OperationCanceledException"/>
        Task<string?> VerifyEmailCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// 해당 이메일이 인증되었는지 확인합니다
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> IsVerifyAsync(string email, CancellationToken cancellationToken = default);
    }
}
