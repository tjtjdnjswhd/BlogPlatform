using BlogPlatform.Shared.Identity.Services.Interfaces;

namespace BlogPlatform.Shared.Identity.Models
{
    /// <summary>
    /// 탈퇴 취소 결과. <see cref="IIdentityService.CancelWithDrawAsync"/> 의 반환값
    /// </summary>
    public enum ECancelWithDrawResult
    {
        /// <summary>
        /// 탈퇴 취소 성공
        /// </summary>
        Success,

        /// <summary>
        /// 해당 유저를 찾을 수 없음
        /// </summary>
        UserNotFound,

        /// <summary>
        /// 복구 가능 시간이 지나 탈퇴 취소가 불가능함
        /// </summary>
        Expired,

        /// <summary>
        /// 탈퇴하지 않은 유저
        /// </summary>
        WithDrawNotRequested,

        /// <summary>
        /// 데이터베이스 오류
        /// </summary>
        DatabaseError
    }
}
