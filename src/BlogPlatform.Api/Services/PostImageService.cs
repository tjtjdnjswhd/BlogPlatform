using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.Extensions.Caching.Distributed;

using System.ComponentModel;
using System.Text.Json;

namespace BlogPlatform.Api.Services
{
    public class PostImageService
    {
        private const string CachePrefix = "post_img_";

        private readonly BlogPlatformImgDbContext _imgDbcontext;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger<PostImageService> _logger;

        public PostImageService(BlogPlatformImgDbContext imgDbcontext, IDistributedCache distributedCache, ILogger<PostImageService> logger)
        {
            _imgDbcontext = imgDbcontext;
            _distributedCache = distributedCache;
            _logger = logger;
        }

        public async Task<ImageInfo?> GetImageAsync(string fileName, EGetImageMode mode, CancellationToken cancellationToken = default)
        {
            switch (mode)
            {
                case EGetImageMode.Cache:
                    return await GetImageFromCacheAsync(fileName, cancellationToken);

                case EGetImageMode.Database:
                    return await GetImageFromDatabaseAsync(fileName, cancellationToken);

                case EGetImageMode.CacheThenDatabase:
                    ImageInfo? image = await GetImageFromCacheAsync(fileName, cancellationToken);
                    return image ?? await GetImageFromDatabaseAsync(fileName, cancellationToken);

                default:
                    throw new InvalidEnumArgumentException(nameof(mode), (int)mode, typeof(EGetImageMode));
            }
        }

        public async Task CacheImageAsync(string fileName, ImageInfo imageInfo, CancellationToken cancellationToken = default)
        {
            byte[] data = JsonSerializer.SerializeToUtf8Bytes(imageInfo);
            await _distributedCache.SetAsync(GetPostImgCacheKey(fileName), data, cancellationToken);
        }

        public async Task CacheImagesToDatabaseAsync(IEnumerable<string> fileNames, CancellationToken cancellationToken = default)
        {
            fileNames.AsParallel().WithCancellation(cancellationToken).ForAll(async fileName =>
            {
                ImageInfo? image = await GetImageFromCacheAsync(fileName, cancellationToken);
                if (image is null)
                {
                    return;
                }

                Image img = new(fileName, image.ContentType, image.Data);
                await _imgDbcontext.Images.AddAsync(img, cancellationToken);
            });

            await _imgDbcontext.SaveChangesAsync(cancellationToken);
        }

        private async Task<ImageInfo?> GetImageFromDatabaseAsync(string fileName, CancellationToken cancellationToken)
        {
            Image? image = await _imgDbcontext.Images.FindAsync([fileName], cancellationToken);
            if (image is null)
            {
                return null;
            }

            ImageInfo imageInfo = new(image.ContentType, image.Data);
            return imageInfo;
        }

        private async Task<ImageInfo?> GetImageFromCacheAsync(string fileName, CancellationToken cancellationToken)
        {
            byte[]? data = await _distributedCache.GetAsync(GetPostImgCacheKey(fileName), cancellationToken);
            ImageInfo? image = JsonSerializer.Deserialize<ImageInfo>(data);
            return image;
        }

        private static string GetPostImgCacheKey(string fileName) => CachePrefix + fileName;
    }
}
