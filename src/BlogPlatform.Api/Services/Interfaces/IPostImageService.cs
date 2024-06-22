using Microsoft.Extensions.Caching.Distributed;

namespace BlogPlatform.Api.Services.Interfaces
{
    public interface IPostImageService
    {
        Task CacheImageAsync(string fileName, ImageInfo imageInfo, DistributedCacheEntryOptions cacheOptions, CancellationToken cancellationToken = default);
        Task<bool> CacheImagesToDatabaseAsync(IEnumerable<string> fileNames, CancellationToken cancellationToken = default);
        Task<ImageInfo?> GetImageAsync(string fileName, EGetImageMode mode, CancellationToken cancellationToken = default);
        Task<ImageInfo?> GetImageFromCacheAsync(string fileName, CancellationToken cancellationToken = default);
        Task<ImageInfo?> GetImageFromDatabaseAsync(string fileName, CancellationToken cancellationToken = default);
        Task RemoveImageFromDatabaseAsync(IEnumerable<string> fileNames, CancellationToken cancellationToken = default);
    }
}