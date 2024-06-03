using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.EFCore.Extensions
{
    public static class SoftDeleteQueryExtensions
    {
        public static IQueryable<T> FilterBySoftDeletedAt<T>(this IQueryable<T> query, DateTimeOffset baseTime, TimeSpan interval)
            where T : EntityBase
        {
            return query.IgnoreSoftDeleteFilter().Where(e => !e.SoftDeletedAt.HasValue || e.SoftDeletedAt.Value.Add(interval) > baseTime);
        }

        public static IQueryable<T> IgnoreSoftDeleteFilter<T>(this IQueryable<T> query)
            where T : EntityBase
        {
            return query.IgnoreQueryFilters();
        }
    }
}
