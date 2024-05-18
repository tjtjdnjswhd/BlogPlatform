using BlogPlatform.EFCore.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BlogPlatform.EFCore.Internals
{
    public class PostLastUpdatedAtInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null)
            {
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            SetLastUpdatedAt(eventData.Context.ChangeTracker);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is null)
            {
                return base.SavingChanges(eventData, result);
            }

            SetLastUpdatedAt(eventData.Context.ChangeTracker);
            return base.SavingChanges(eventData, result);
        }

        private static void SetLastUpdatedAt(ChangeTracker changeTracker)
        {
            foreach (var entry in changeTracker.Entries<Post>()
                .Where(e => e.State == EntityState.Modified
                && (e.Property(p => p.Title).IsModified || e.Property(p => p.Content).IsModified || e.Property(p => p.Tags).IsModified)))
            {
                entry.Entity.LastUpdatedAt = DateTimeOffset.Now;
            }
        }
    }
}
