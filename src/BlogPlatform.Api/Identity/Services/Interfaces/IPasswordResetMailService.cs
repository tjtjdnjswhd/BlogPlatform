namespace BlogPlatform.Api.Identity.Services.Interfaces
{
    /// <summary>
    /// 비밀번호 초기화 이메일 서비스
    /// </summary>
    public interface IPasswordResetMailService
    {
        /// <summary>
        /// 해당 <paramref name="email"/>로 초기화된 비밀번호를 전송합니다
        /// </summary>
        /// <param name="email">비밀번호를 초기화 한 유저의 이메일</param>
        /// <param name="newPassword">초기화된 신규 이메일</param>
        void SendResetPasswordEmail(string email, string newPassword);
    }
}
