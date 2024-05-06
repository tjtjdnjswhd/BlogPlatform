using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

namespace BlogPlatform.EFCore.Models
{
    /// <summary>
    /// 유저 역할
    /// </summary>
    [Index(nameof(Name), nameof(DeletedAt), IsUnique = true)]
    [Index(nameof(Priority), nameof(DeletedAt), IsUnique = true)]
    [Table("Role")]
    public class Role : EntityBase
    {
        public Role(string name, int priority)
        {
            Name = name;
            Priority = priority;
        }

        /// <summary>
        /// 이름
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 우선순위
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 사용자
        /// </summary>
        public List<User> Users { get; set; }
    }
}
