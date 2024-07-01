namespace BlogPlatform.Shared.Identity.Models
{
    public enum EWithDrawResult
    {
        /// <summary>
        /// 탈퇴 성공
        /// </summary>
        Success,

        /// <summary>
        /// 해당 유저를 찾을 수 없음
        /// </summary>
        UserNotFound,

        /// <summary>
        /// 데이터베이스 오류
        /// </summary>
        DatabaseError
    }
}
