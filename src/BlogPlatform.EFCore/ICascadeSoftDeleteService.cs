using BlogPlatform.EFCore.Models.Abstractions;

using StatusGeneric;

namespace BlogPlatform.EFCore
{
    public interface ICascadeSoftDeleteService
    {
        IStatusGeneric<int> SetSoftDelete<T>(T entity, bool callSaveChanges) where T : EntityBase;
        Task<IStatusGeneric<int>> SetSoftDeleteAsync<T>(T entity, bool callSaveChanges) where T : EntityBase;
        IStatusGeneric<int> ResetSoftDelete<T>(T entity, bool callSaveChanges) where T : EntityBase;
        Task<IStatusGeneric<int>> ResetSoftDeleteAsync<T>(T entity, bool callSaveChanges) where T : EntityBase;
        IStatusGeneric<int> CheckSoftDelete<T>(T entity) where T : EntityBase;
        Task<IStatusGeneric<int>> CheckSoftDeleteAsync<T>(T entity) where T : EntityBase;
    }
}