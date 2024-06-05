using BlogPlatform.EFCore.Models.Abstractions;

namespace BlogPlatform.EFCore.Extensions
{
    public static class EntityBaseExtensions
    {
        public static bool IsSoftDeletedAtDefault(this EntityBase entity)
        {
            return EntityBase.DefaultSoftDeletedAt.Subtract(TimeSpan.FromMilliseconds(1)) <= entity.SoftDeletedAt && entity.SoftDeletedAt <= EntityBase.DefaultSoftDeletedAt;
        }
    }
}
