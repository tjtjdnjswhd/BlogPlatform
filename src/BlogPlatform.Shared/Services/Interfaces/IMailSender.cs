namespace BlogPlatform.Shared.Services.Interfaces
{
    /// <summary>
    /// 이메일 전송을 담당하는 서비스
    /// </summary>
    public interface IMailSender
    {
        /// <summary>
        /// <paramref name="to"/>로 이메일을 전송합니다
        /// </summary>
        /// <param name="context">이메일 전송에 필요한 정보</param>
        /// <param name="cancellationToken"></param>
        /// 
        void Send(MailSendContext context, CancellationToken cancellationToken = default);
    }
}
