namespace BlogPlatform.Api.Identity.Services.Interfaces
{
    /// <summary>
    /// 이메일 인증 서비스
    /// </summary>
    public interface IVerifyEmailService
    {
        /// <summary>
        /// 인증 메일을 <paramref name="email"/>로 전송합니다
        /// </summary>
        /// <param name="email">인증 대상 이메일</param>
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
        /// <returns>
        /// true: 인증됨
        /// false: 인증되지 않음
        /// </returns>
        /// <exception cref="OperationCanceledException"/>
        Task<bool> IsVerifyAsync(string email, CancellationToken cancellationToken = default);
    }
}
