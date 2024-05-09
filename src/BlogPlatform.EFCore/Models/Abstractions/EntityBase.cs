using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.EFCore.Models.Abstractions
{
    [PrimaryKey(nameof(Id))]
    [Index(nameof(DeletedAt))]
    public abstract class EntityBase
    {
        /// <summary>
        /// 고유 식별자
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// 생성 시각
        /// </summary>
        [Required]
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>
        /// 삭제 시각
        /// </summary>
        [Required]
        public DateTimeOffset? DeletedAt { get; internal set; }

        [Timestamp]
        public byte[] Version { get; private set; }
    }
}
