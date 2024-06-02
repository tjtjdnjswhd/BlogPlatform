using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogPlatform.EFCore.Models
{
    /// <summary>
    /// 블로그
    /// </summary>
    [Index(nameof(Name), nameof(SoftDeletedAt), IsUnique = true)]
    [Index(nameof(UserId), nameof(SoftDeletedAt), IsUnique = true)]
    [Table("Blog")]
    public class Blog : EntityBase
    {
        public Blog(string name, string description, int userId)
        {
            Name = name;
            Description = description;
            UserId = userId;
        }

        /// <summary>
        /// 블로그 이름
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// 블로그 설명
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// 사용자 ID
        /// </summary>
        [Required]
        public int UserId { get; private set; }

        /// <summary>
        /// 사용자
        /// </summary>
        public User User { get; private set; }

        /// <summary>
        /// 카테고리
        /// </summary>
        public List<Category> Categories { get; } = [];
    }
}
