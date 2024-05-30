namespace BlogPlatform.Api.Services.Interfaces
{
    /// <summary>
    /// 이메일 전송을 담당하는 서비스
    /// </summary>
    public interface IMailSender
    {
        /// <summary>
        /// <paramref name="to"/>로 이메일을 전송합니다
        /// </summary>
        /// <param name="from">발신자</param>
        /// <param name="to">수신자</param>
        /// <param name="subject">제목</param>
        /// <param name="body">이메일 본문</param>
        /// <param name="cancellationToken"></param>
        void Send(string from, string to, string subject, string body, CancellationToken cancellationToken = default);
    }
}
