using BlogPlatform.EFCore.Models.Abstractions;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogPlatform.EFCore.Models
{
    /// <summary>
    /// 블로그 게시글
    /// </summary>
    [Table("Post")]
    public class Post : EntityBase
    {
        public Post(string title, string content, DateTimeOffset lastUpdatedAt, int categoryId)
        {
            Title = title;
            Content = content;
            LastUpdatedAt = lastUpdatedAt;
            CategoryId = categoryId;
        }

        /// <summary>
        /// 제목
        /// </summary>
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// 내용
        /// </summary>
        [Required]
        public string Content { get; set; }

        [Column("json")]
        public List<string> Tags { get; } = [];

        /// <summary>
        /// 마지막 수정 시각
        /// </summary>
        [Required]
        public DateTimeOffset LastUpdatedAt { get; set; }

        /// <summary>
        /// 카테고리 ID
        /// </summary>
        [Required]
        public int CategoryId { get; set; }

        /// <summary>
        /// 카테고리
        /// </summary>
        public Category Category { get; set; }

        /// <summary>
        /// 댓글
        /// </summary>
        public List<Comment> Comments { get; } = [];
    }
}
