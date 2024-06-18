using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.EFCore.Extensions
{
    public static class SoftDeleteQueryExtensions
    {
        public static IQueryable<T> FilterBySoftDeletedAt<T>(this IQueryable<T> query, DateTimeOffset baseTime, TimeSpan interval)
            where T : EntityBase
        {
            DateTimeOffset date = baseTime.Subtract(interval);
            return query.IgnoreSoftDeleteFilter().Where(e => e.SoftDeletedAt > date);
        }

        public static IQueryable<T> IgnoreSoftDeleteFilter<T>(this IQueryable<T> query)
            where T : EntityBase
        {
            return query.IgnoreQueryFilters();
        }

        public static IQueryable<T> FilterSoftDeletedEntity<T>(this IQueryable<T> query)
            where T : EntityBase
        {
            return query.Where(e => e.SoftDeleteLevel == 0);
        }
    }
}
