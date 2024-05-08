using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogPlatform.EFCore.Models
{
    /// <summary>
    /// 사용자
    /// </summary>
    [Index(nameof(Name), nameof(DeletedAt), IsUnique = true)]
    [Index(nameof(Email), nameof(DeletedAt), IsUnique = true)]
    [Table("User")]
    public class User : EntityBase
    {
        public User(string name, string email, int? basicLoginAccountId)
        {
            Name = name;
            Email = email;
            BasicLoginAccountId = basicLoginAccountId;
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
        /// 기본 로그인 계정 ID
        /// </summary>
        public int? BasicLoginAccountId { get; set; }

        /// <summary>
        /// 블로그
        /// </summary>
        public Blog? Blog { get; private set; }

        /// <summary>
        /// 댓글
        /// </summary>
        public List<Comment> Comments { get; } = [];

        /// <summary>
        /// 기본 로그인 계정
        /// </summary>
        public BasicAccount? BasicLoginAccount { get; set; }

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
