using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogPlatform.EFCore.Models
{
    /// <summary>
    /// 사용자
    /// </summary>
    [Index(nameof(Name), nameof(SoftDeletedAt), IsUnique = true)]
    [Index(nameof(Email), nameof(SoftDeletedAt), IsUnique = true)]
    [Table("User")]
    public class User : EntityBase
    {
        public User(string name, string email)
        {
            Name = name;
            Email = email;
        }

        /// <summary>
        /// 사용자 이름
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// 이메일
        /// </summary>
        [Required]
        public string Email { get; set; }

        /// <summary>
        /// 차단 해제 시간
        /// </summary>
        public DateTimeOffset? BanExpiresAt { get; set; }

        /// <summary>
        /// 블로그
        /// </summary>
        public List<Blog> Blog { get; private set; } = [];

        /// <summary>
        /// 댓글
        /// </summary>
        public List<Comment> Comments { get; } = [];

        /// <summary>
        /// 기본 로그인 계정
        /// </summary>
        public List<BasicAccount> BasicAccounts { get; } = [];

        /// <summary>
        /// OAuth 계정
        /// </summary>
        public List<OAuthAccount> OAuthAccounts { get; } = [];

        /// <summary>
        /// 역할
        /// </summary>
        public List<Role> Roles { get; } = [];
    }
}
