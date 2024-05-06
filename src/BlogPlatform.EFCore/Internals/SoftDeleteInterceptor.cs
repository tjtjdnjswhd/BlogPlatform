using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BlogPlatform.EFCore.Internals
{
    internal class SoftDeleteInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is null)
            {
                return base.SavingChanges(eventData, result);
            }

            foreach (var entity in eventData.Context.ChangeTracker.Entries<EntityBase>().Where(e => e.State == EntityState.Deleted))
            {
                entity.State = EntityState.Modified;
                entity.Entity.DeletedAt = DateTimeOffset.Now;
            }

            return base.SavingChanges(eventData, result);
        }
    }
}
