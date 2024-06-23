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
        public Post(string title, string content, int categoryId)
        {
            Title = title;
            Content = content;
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
        [Column(TypeName = "text")]
        public string Content { get; set; }

        /// <summary>
        /// 태그
        /// </summary>
        [Column(TypeName = "json")]
        public List<string> Tags { get; } = [];

        /// <summary>
        /// 마지막 수정 시각
        /// </summary>
        public DateTimeOffset? LastUpdatedAt { get; set; }

        /// <summary>
        /// 카테고리 ID
        /// </summary>
        [Required]
        public int CategoryId { get; set; }

        /// <summary>
        /// 카테고리
        /// </summary>
        public Category Category { get; set; }

        /*
        ----------------------------
        Collection navigation의 경우 CascadeSoftDeleteService.ResetSoftDelete(), ResetSoftDeleteAsync() 메서드와의 호환성을 위해 생성 시 null이어야 함

        ex)  
            X public List<Blog> Blog { get; set; } = [];
            X private List<Blog> _blog;
               public List<Blog> Blog => _blog ??= [];
            O public List<Blog> Blog { get; set; }
            O public List<Blog> Blog { get; private set; }
        ----------------------------
         */

        /// <summary>
        /// 댓글
        /// </summary>
        public List<Comment> Comments { get; private set; }
    }
}
