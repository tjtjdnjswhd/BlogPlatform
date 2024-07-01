using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Identity.Options
{
    /// <summary>
    /// 계정 설정
    /// </summary>
    public class AccountOptions
    {
        /// <summary>
        /// 최소 Id 길이
        /// </summary>
        [Range(1, int.MaxValue)]
        public required int MinIdLength { get; set; }

        /// <summary>
        /// 최대 Id 길이
        /// </summary>
        [Range(1, int.MaxValue)]
        public required int MaxIdLength { get; set; }

        /// <summary>
        /// 최소 password 길이
        /// </summary>
        [Range(1, int.MaxValue)]
        public required int MinPasswordLength { get; set; }

        /// <summary>
        /// 최대 password 길이
        /// </summary>
        [Range(1, int.MaxValue)]
        public required int MaxPasswordLength { get; set; }

        /// <summary>
        /// 최소 name 길이
        /// </summary>
        [Range(1, int.MaxValue)]
        public required int MinNameLength { get; set; }

        /// <summary>
        /// 최대 name 길이
        /// </summary>
        [Range(1, int.MaxValue)]
        public required int MaxNameLength { get; set; }
    }
}
