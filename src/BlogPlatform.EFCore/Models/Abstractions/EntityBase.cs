﻿using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;

namespace BlogPlatform.EFCore.Models.Abstractions
{
    [PrimaryKey(nameof(Id))]
    [Index(nameof(SoftDeleteLevel))]
    public abstract class EntityBase
    {
        public static readonly DateTimeOffset DefaultSoftDeletedAt = DateTimeOffset.MaxValue;

        /// <summary>
        /// 고유 식별자
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// 생성 시각
        /// </summary>
        [Required]
        public DateTimeOffset CreatedAt { get; internal set; }

        /// <summary>
        /// 삭제 시각
        /// </summary>
        [Required]
        public DateTimeOffset SoftDeletedAt { get; internal set; }

        /// <summary>
        /// Soft Delete 레벨
        /// </summary>
        [Required]
        public byte SoftDeleteLevel { get; internal set; }

        [Timestamp]
        public byte[] RowVersion { get; private set; }
    }
}
