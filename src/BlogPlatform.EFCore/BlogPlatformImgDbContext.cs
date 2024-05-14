using BlogPlatform.EFCore.Models;

using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.EFCore
{
    public class BlogPlatformImgDbContext : DbContext
    {
        public DbSet<Image> Images { get; private set; }

        public BlogPlatformImgDbContext(DbContextOptions<BlogPlatformImgDbContext> options) : base(options)
        {
        }
    }
}
