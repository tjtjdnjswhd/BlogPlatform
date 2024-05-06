using BlogPlatform.EFCore.Models.Abstractions;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogPlatform.EFCore.Models
{
    [Table("Category")]
    public class Category : EntityBase
    {
        public Category(string name, int blogId)
        {
            Name = name;
            BlogId = blogId;
        }

        [Required]
        public string Name { get; set; }

        [Required]
        public int BlogId { get; set; }

        public Blog Blog { get; set; }

        public List<Post> Posts { get; } = [];
    }
}
