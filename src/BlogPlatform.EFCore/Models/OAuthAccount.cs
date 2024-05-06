using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogPlatform.EFCore.Models
{
    /// <summary>
    /// OAuth 계정
    /// </summary>
    [Index(nameof(NameIdentifier), nameof(ProviderId), nameof(DeletedAt), IsUnique = true)]
    [Table("OAuthAccount")]
    public class OAuthAccount : EntityBase
    {
        public OAuthAccount(string nameIdentifier, int providerId, int userId)
        {
            NameIdentifier = nameIdentifier;
            ProviderId = providerId;
            UserId = userId;
        }

        /// <summary>
        /// OAuth 식별자
        /// </summary>
        [Required]
        public string NameIdentifier { get; private set; }

        /// <summary>
        /// OAuth 제공자 ID
        /// </summary>
        [Required]
        public int ProviderId { get; private set; }

        /// <summary>
        /// 사용자 ID
        /// </summary>
        [Required]
        public int UserId { get; private set; }

        /// <summary>
        /// OAuth 제공자
        /// </summary>
        public OAuthProvider Provider { get; private set; }

        /// <summary>
        /// 사용자
        /// </summary>
        public User User { get; private set; }
    }
}
