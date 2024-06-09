namespace BlogPlatform.Api.Models
{
    public class CommentSearch
    {
        public string? Content { get; set; }

        public int? PostId { get; set; }

        public int? UserId { get; set; }

        public int Page { get; set; } = 1;
    }
}
