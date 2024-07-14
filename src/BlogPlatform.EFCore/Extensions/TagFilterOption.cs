namespace BlogPlatform.EFCore.Extensions
{
    public enum TagFilterOption
    {
        /// <summary>
        /// 해당 태그를 모두 포함하는 게시물을 검색합니다
        /// </summary>
        All,

        /// <summary>
        /// 해당 태그 중 하나라도 포함하는 게시물을 검색합니다
        /// </summary>
        Any
    }
}
