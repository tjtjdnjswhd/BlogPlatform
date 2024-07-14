namespace BlogPlatform.Shared.Models.Admin
{
    /// <summary>
    /// Admin 이메일 전송 모델
    /// </summary>
    /// <param name="Subject">이메일 제목</param>
    /// <param name="Body">이메일 본문</param>
    /// <param name="UserIds">메일을 보낼 유저 목록. null일 경우 모든 유저에게 메일을 보냅니다</param>
    public record SendMailModel(string Subject, string Body, List<int>? UserIds);
}
