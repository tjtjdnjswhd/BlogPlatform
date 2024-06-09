using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;

using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;

namespace BlogPlatform.Api.Services
{
    public class PostImageService : IPostImageService
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
            _logger.LogInformation("Getting image {fileName} with mode {mode}", fileName, mode);
            ImageInfo? image = mode switch
            {
                EGetImageMode.Cache => await GetImageFromCacheAsync(fileName, cancellationToken),
                EGetImageMode.Database => await GetImageFromDatabaseAsync(fileName, cancellationToken),
                EGetImageMode.CacheThenDatabase => await GetImageFromCacheAsync(fileName, cancellationToken) ?? await GetImageFromDatabaseAsync(fileName, cancellationToken),
                _ => throw new InvalidEnumArgumentException(nameof(mode), (int)mode, typeof(EGetImageMode))
            };

            _logger.LogInformation("Image {fileName} with mode {mode} is {state}", fileName, mode, image is null ? "not exist" : "exist");
            return image;
        }

        public async Task CacheImageAsync(string fileName, ImageInfo imageInfo, DistributedCacheEntryOptions cacheOptions, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Caching image {fileName}", fileName);
            byte[] data = JsonSerializer.SerializeToUtf8Bytes(imageInfo);
            await _distributedCache.SetAsync(GetPostImgCacheKey(fileName), data, cacheOptions, cancellationToken);
        }

        public async Task<bool> CacheImagesToDatabaseAsync(IEnumerable<string> fileNames, CancellationToken cancellationToken = default)
        {
            (string fileName, ImageInfo?)[] images = await Task.WhenAll(fileNames.AsParallel().Select(async fileName => (fileName, await GetImageFromCacheAsync(fileName, cancellationToken))).ToList());

            if (images.Any(image => image.Item2 is null))
            {
                _logger.LogWarning("Some images not found in cache. Saving images to database aborted.");
                return false;
            }

            foreach ((string fileName, ImageInfo? image) in images)
            {
                Image img = new(fileName, image!.ContentType, image.Data);
                _imgDbcontext.Images.Add(img);
            }

            await _imgDbcontext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<ImageInfo?> GetImageFromDatabaseAsync(string fileName, CancellationToken cancellationToken = default)
        {
            Image? image = await _imgDbcontext.Images.FindAsync([fileName], cancellationToken);
            if (image is null)
            {
                _logger.LogInformation("Image {fileName} not found in database", fileName);
                return null;
            }

            ImageInfo imageInfo = new(image.ContentType, image.Data);
            _logger.LogInformation("Image {fileName} found in database", fileName);
            return imageInfo;
        }

        public async Task<ImageInfo?> GetImageFromCacheAsync(string fileName, CancellationToken cancellationToken = default)
        {
            byte[]? data = await _distributedCache.GetAsync(GetPostImgCacheKey(fileName), cancellationToken);
            if (data is null)
            {
                _logger.LogInformation("Image {fileName} not found in cache", fileName);
                return null;
            }

            ImageInfo? image = JsonSerializer.Deserialize<ImageInfo>(data);
            Debug.Assert(image is not null);
            _logger.LogInformation("Image {fileName} found in cache", fileName);
            return image;
        }

        public async Task RemoveImageFromDatabaseAsync(IEnumerable<string> fileNames, CancellationToken cancellationToken = default)
        {
            await _imgDbcontext.Images.Where(i => fileNames.Contains(i.Name)).ExecuteDeleteAsync(cancellationToken);
            return;
        }

        public IPostImageService WithTransaction(IDbContextTransaction transaction)
        {
            _imgDbcontext.Database.UseTransaction(transaction.GetDbTransaction());
            return this;
        }

        private static string GetPostImgCacheKey(string fileName) => CachePrefix + fileName;
    }
}
