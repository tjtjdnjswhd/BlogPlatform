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

        public List<Post> Posts { get; private set; }
    }
}
