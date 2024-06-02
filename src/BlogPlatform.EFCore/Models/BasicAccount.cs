using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogPlatform.EFCore.Models
{
    /// <summary>
    /// 기본 계정
    /// </summary>
    [Index(nameof(AccountId), nameof(SoftDeletedAt), IsUnique = true)]
    [Table("BasicAccounts")]
    public class BasicAccount : EntityBase
    {
        public BasicAccount(string accountId, string passwordHash)
        {
            AccountId = accountId;
            PasswordHash = passwordHash;
        }

        /// <summary>
        /// 계정 ID
        /// </summary>
        [Required]
        public string AccountId { get; private set; }

        /// <summary>
        /// 비밀번호 해시
        /// </summary>
        [Required]
        public string PasswordHash { get; set; }

        /// <summary>
        /// 비밀번호 변경 필요 여부
        /// </summary>
        [Required]
        public bool IsPasswordChangeRequired { get; set; }

        /// <summary>
        /// 사용자
        /// </summary>
        public User User { get; private set; }
    }
}
