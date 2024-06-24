using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.Extensions.Logging;

using SoftDeleteServices.Concrete;

using StatusGeneric;

namespace BlogPlatform.EFCore
{
    public class CascadeSoftDeleteService : ICascadeSoftDeleteService
    {
        private readonly BlogPlatformDbContext _dbContext;

        private readonly SoftDeleteConfigure _softDeleteConfigure;

        private CascadeSoftDelService<EntityBase>? _cascadeSoftDelService;
        private CascadeSoftDelService<EntityBase> CascadeSoftDelService => _cascadeSoftDelService ??= new(_softDeleteConfigure);

        private CascadeSoftDelServiceAsync<EntityBase>? _cascadeSoftDelServiceAsync;
        private CascadeSoftDelServiceAsync<EntityBase> CascadeSoftDelServiceAsync => _cascadeSoftDelServiceAsync ??= new(_softDeleteConfigure);

        private readonly ILogger<CascadeSoftDeleteService> _logger;

        public CascadeSoftDeleteService(BlogPlatformDbContext dbContext, TimeProvider timeProvider, ILogger<CascadeSoftDeleteService> logger)
        {
            _dbContext = dbContext;
            _softDeleteConfigure = new(dbContext, timeProvider);
            _logger = logger;
        }

        public IStatusGeneric<int> SetSoftDelete<T>(T entity, bool callSaveChanges)
            where T : EntityBase
        {
            _dbContext.Set<T>().Attach(entity);
            _logger.LogDebug("Sync soft deleting entity with Id: {id}. type: {type}", entity.Id, typeof(T).Name);
            return CascadeSoftDelService.SetCascadeSoftDelete(entity, callSaveChanges);
        }

        public async Task<IStatusGeneric<int>> SetSoftDeleteAsync<T>(T entity, bool callSaveChanges)
            where T : EntityBase
        {
            _dbContext.Set<T>().Attach(entity);
            _logger.LogDebug("Async soft deleting entity with Id: {id}. type: {type}", entity.Id, typeof(T).Name);
            return await CascadeSoftDelServiceAsync.SetCascadeSoftDeleteAsync(entity, callSaveChanges);
        }

        public IStatusGeneric<int> ResetSoftDelete<T>(T entity, bool callSaveChanges)
            where T : EntityBase
        {
            _dbContext.Set<T>().Attach(entity);
            _logger.LogDebug("Sync resetting soft delete for entity with Id: {id}. type: {type}", entity.Id, typeof(T).Name);
            return CascadeSoftDelService.ResetCascadeSoftDelete(entity, callSaveChanges);
        }

        public async Task<IStatusGeneric<int>> ResetSoftDeleteAsync<T>(T entity, bool callSaveChanges)
            where T : EntityBase
        {
            _dbContext.Set<T>().Attach(entity);
            _logger.LogDebug("Async resetting soft delete for entity with Id: {id}. type: {type}", entity.Id, typeof(T).Name);
            return await CascadeSoftDelServiceAsync.ResetCascadeSoftDeleteAsync(entity, callSaveChanges);
        }

        public IStatusGeneric<int> CheckSoftDelete<T>(T entity)
            where T : EntityBase
        {
            _dbContext.Set<T>().Attach(entity);
            _logger.LogDebug("Sync Checking soft delete for entity with Id: {id}. type: {type}", entity.Id, typeof(T).Name);
            return CascadeSoftDelService.CheckCascadeSoftDelete(entity);
        }

        public async Task<IStatusGeneric<int>> CheckSoftDeleteAsync<T>(T entity)
            where T : EntityBase
        {
            _dbContext.Set<T>().Attach(entity);
            _logger.LogDebug("Async Checking soft delete for entity with Id: {id}. type: {type}", entity.Id, typeof(T).Name);
            return await CascadeSoftDelServiceAsync.CheckCascadeSoftDeleteAsync(entity);
        }
    }
}
