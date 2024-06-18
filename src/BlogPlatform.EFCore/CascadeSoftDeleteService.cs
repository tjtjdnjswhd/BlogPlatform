using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.Extensions.Logging;

using SoftDeleteServices.Concrete;

using StatusGeneric;

namespace BlogPlatform.EFCore
{
    public class CascadeSoftDeleteService : ICascadeSoftDeleteService
    {
        private readonly BlogPlatformDbContext _dbContext;
        private readonly CascadeSoftDelService<EntityBase> _cascadeSoftDelService;
        private readonly CascadeSoftDelServiceAsync<EntityBase> _cascadeSoftDelServiceAsync;
        private readonly ILogger<CascadeSoftDeleteService> _logger;

        public CascadeSoftDeleteService(BlogPlatformDbContext dbContext, ILogger<CascadeSoftDeleteService> logger)
        {
            _dbContext = dbContext;
            _cascadeSoftDelService = new(new SoftDeleteConfigure(dbContext));
            _cascadeSoftDelServiceAsync = new(new SoftDeleteConfigure(dbContext));
            _logger = logger;
        }

        public IStatusGeneric<int> SetSoftDelete<T>(T entity, bool callSaveChanges)
            where T : EntityBase
        {
            _dbContext.Set<T>().Attach(entity);
            _logger.LogDebug("Sync soft deleting entity with Id: {id}. type: {type}", entity.Id, typeof(T).Name);
            return _cascadeSoftDelService.SetCascadeSoftDelete(entity, callSaveChanges);
        }

        public async Task<IStatusGeneric<int>> SetSoftDeleteAsync<T>(T entity, bool callSaveChanges)
            where T : EntityBase
        {
            _dbContext.Set<T>().Attach(entity);
            _logger.LogDebug("Async soft deleting entity with Id: {id}. type: {type}", entity.Id, typeof(T).Name);
            return await _cascadeSoftDelServiceAsync.SetCascadeSoftDeleteAsync(entity, callSaveChanges);
        }

        public IStatusGeneric<int> ResetSoftDelete<T>(T entity, bool callSaveChanges)
            where T : EntityBase
        {
            _dbContext.Set<T>().Attach(entity);
            _logger.LogDebug("Sync resetting soft delete for entity with Id: {id}. type: {type}", entity.Id, typeof(T).Name);
            return _cascadeSoftDelService.ResetCascadeSoftDelete(entity, callSaveChanges);
        }

        public async Task<IStatusGeneric<int>> ResetSoftDeleteAsync<T>(T entity, bool callSaveChanges)
            where T : EntityBase
        {
            _dbContext.Set<T>().Attach(entity);
            _logger.LogDebug("Async resetting soft delete for entity with Id: {id}. type: {type}", entity.Id, typeof(T).Name);
            return await _cascadeSoftDelServiceAsync.ResetCascadeSoftDeleteAsync(entity, callSaveChanges);
        }

        public IStatusGeneric<int> CheckSoftDelete<T>(T entity)
            where T : EntityBase
        {
            _dbContext.Set<T>().Attach(entity);
            _logger.LogDebug("Sync Checking soft delete for entity with Id: {id}. type: {type}", entity.Id, typeof(T).Name);
            return _cascadeSoftDelService.CheckCascadeSoftDelete(entity);
        }

        public async Task<IStatusGeneric<int>> CheckSoftDeleteAsync<T>(T entity)
            where T : EntityBase
        {
            _dbContext.Set<T>().Attach(entity);
            _logger.LogDebug("Async Checking soft delete for entity with Id: {id}. type: {type}", entity.Id, typeof(T).Name);
            return await _cascadeSoftDelServiceAsync.CheckCascadeSoftDeleteAsync(entity);
        }
    }
}
