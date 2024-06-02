using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogPlatform.EFCore.Models
{
    /// <summary>
    /// OAuth 제공자
    /// </summary>
    [Index(nameof(Name), nameof(SoftDeletedAt), IsUnique = true)]
    [Table("OAuthProvider")]
    public class OAuthProvider : EntityBase
    {
        public OAuthProvider(string name)
        {
            Name = name;
        }

        /// <summary>
        /// OAuth 제공자 이름
        /// </summary>
        [Required]
        public string Name { get; set; }
    }
}
