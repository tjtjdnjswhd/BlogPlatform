namespace BlogPlatform.Shared.Identity.Services.Interfaces
{
    /// <summary>
    /// 이메일 인증 서비스
    /// </summary>
    public interface IEmailVerifyService
    {
        /// <summary>
        /// 인증 코드를 생성합니다
        /// </summary>
        /// <returns></returns>
        string GenerateVerificationCode();

        /// <summary>
        /// 회원 가입용 인증 코드를 설정합니다
        /// </summary>
        /// <param name="email"></param>
        /// <param name="code"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetSignUpVerifyCodeAsync(string email, string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// 회원 가입용 인증 코드를 확인합니다
        /// </summary>
        /// <param name="code"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 인증 성공 시 이메일을 반환합니다.
        /// 실패 시 null을 반환합니다.
        /// </returns>
        Task<string?> VerifySignUpEmailCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// 해당 이메일이 회원 가입용으로 인증되었는지 확인합니다
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> IsSignUpEmailVerifiedAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// 이메일 변경용 인증 코드를 설정합니다
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="email"></param>
        /// <param name="code"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetChangeVerifyCodeAsync(int userId, string email, string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// 이메일 변경용 인증 코드를 확인합니다
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="code"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 인증 성공 시 이메일을 반환합니다.
        /// 실패 시 null을 반환합니다
        /// </returns>
        Task<string?> VerifyChangeEmailCodeAsync(int userId, string code, CancellationToken cancellationToken = default);
    }
}