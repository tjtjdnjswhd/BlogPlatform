namespace BlogPlatform.Api.Identity.Services.Interfaces
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
        /// 인증 코드를 설정합니다
        /// </summary>
        /// <param name="email"></param>
        /// <param name="code"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SetVerifyCodeAsync(string email, string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// 인증 코드를 확인합니다
        /// </summary>
        /// <param name="code"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>
        /// 인증 성공 시 이메일을 반환합니다.
        /// 실패 시, null을 반환합니다.
        /// </returns>
        Task<string?> VerifyEmailCodeAsync(string code, CancellationToken cancellationToken = default);

        /// <summary>
        /// 해당 이메일이 인증되었는지 확인합니다
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<bool> IsEmailVerifiedAsync(string email, CancellationToken cancellationToken = default);
    }
}