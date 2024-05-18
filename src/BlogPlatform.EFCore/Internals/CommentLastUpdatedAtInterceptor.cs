using BlogPlatform.EFCore.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BlogPlatform.EFCore.Internals
{
    public class CommentLastUpdatedAtInterceptor : SaveChangesInterceptor
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
            foreach (var entry in changeTracker.Entries<Comment>().Where(e => e.State == EntityState.Modified && e.Property(c => c.Content).IsModified))
            {
                entry.Entity.LastUpdatedAt = DateTimeOffset.Now;
            }
        }
    }
}
