namespace BlogPlatform.Api.Identity.Services.Interfaces
{
    /// <summary>
    /// 계정 ID 찾기 이메일 서비스
    /// </summary>
    public interface IFindAccountIdMailService
    {
        void SendMail(string email, string accountId);
    }
}
