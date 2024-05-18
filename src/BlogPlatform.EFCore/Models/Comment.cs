using BlogPlatform.EFCore.Models.Abstractions;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogPlatform.EFCore.Models
{
    /// <summary>
    /// 게시글 댓글
    /// </summary>
    [Table("Comment")]
    public class Comment : EntityBase
    {
        public Comment(string content, int postId, int userId, int? parentCommentId)
        {
            Content = content;
            PostId = postId;
            UserId = userId;
            ParentCommentId = parentCommentId;
        }

        /// <summary>
        /// 댓글 내용
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// 마지막 수정 시각
        /// </summary>
        public DateTimeOffset? LastUpdatedAt { get; set; }

        /// <summary>
        /// 게시글 ID
        /// </summary>
        [Required]
        public int PostId { get; private set; }

        /// <summary>
        /// 사용자 ID
        /// </summary>
        [Required]
        public int UserId { get; private set; }

        /// <summary>
        /// 부모 댓글 ID
        /// </summary>
        public int? ParentCommentId { get; private set; }

        /// <summary>
        /// 유저
        /// </summary>
        public User User { get; private set; }

        /// <summary>
        /// 게시글
        /// </summary>
        public Post Post { get; private set; }

        /// <summary>
        /// 부모 댓글
        /// </summary>
        public Comment? ParentComment { get; private set; }

        /// <summary>
        /// 자식 댓글
        /// </summary>
        public List<Comment> ChildComments { get; } = [];
    }
}
