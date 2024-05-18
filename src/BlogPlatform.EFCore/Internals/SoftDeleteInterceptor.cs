﻿using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BlogPlatform.EFCore.Internals
{
    internal class SoftDeleteInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null)
            {
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            SetDeletedAt(eventData.Context.ChangeTracker);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is null)
            {
                return base.SavingChanges(eventData, result);
            }

            SetDeletedAt(eventData.Context.ChangeTracker);
            return base.SavingChanges(eventData, result);
        }

        private static void SetDeletedAt(ChangeTracker changeTracker)
        {
            foreach (var entry in changeTracker.Entries<EntityBase>().Where(e => e.State == EntityState.Deleted))
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedAt = DateTimeOffset.Now;
            }
        }
    }
}
