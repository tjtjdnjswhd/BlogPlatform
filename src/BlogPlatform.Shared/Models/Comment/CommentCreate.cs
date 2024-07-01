using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.Shared.Models.Comment
{
    public record CommentCreate([Required(AllowEmptyStrings = false)] string Content, int? PostId, int? ParentCommentId) : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!(PostId is null ^ ParentCommentId is null))
            {
                yield return new ValidationResult("댓글은 게시글 또는 다른 댓글에 속해야 합니다.", [nameof(PostId), nameof(ParentCommentId)]);
            }
        }
    }
}