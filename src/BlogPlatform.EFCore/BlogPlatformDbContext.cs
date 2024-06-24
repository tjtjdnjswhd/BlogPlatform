using BlogPlatform.EFCore.Internals;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

using System.Linq.Expressions;
using System.Text.Json;

namespace BlogPlatform.EFCore
{
    public class BlogPlatformDbContext : DbContext
    {
        public virtual DbSet<Blog> Blogs { get; private set; }

        public virtual DbSet<Category> Categories { get; private set; }

        public virtual DbSet<Post> Posts { get; private set; }

        public virtual DbSet<Comment> Comments { get; private set; }

        public virtual DbSet<User> Users { get; private set; }

        public virtual DbSet<BasicAccount> BasicAccounts { get; private set; }

        public virtual DbSet<OAuthAccount> OAuthAccounts { get; private set; }

        public virtual DbSet<OAuthProvider> OAuthProviders { get; private set; }

        public virtual DbSet<Role> Roles { get; private set; }

        private readonly TimeProvider _timeProvider;

        public BlogPlatformDbContext(DbContextOptions<BlogPlatformDbContext> options) : base(options)
        {
            TimeProvider? timeProvider = options.FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider?.GetRequiredService<TimeProvider>();
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(new PostLastUpdatedAtInterceptor(_timeProvider));
            optionsBuilder.AddInterceptors(new CommentLastUpdatedAtInterceptor(_timeProvider));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Blog>(builder =>
            {
                builder.HasOne(b => b.User).WithMany(u => u.Blog).HasForeignKey(b => b.UserId);
            });

            modelBuilder.Entity<Category>(builder =>
            {
                builder.HasOne(c => c.Blog).WithMany(b => b.Categories).HasForeignKey(c => c.BlogId);
            });

            modelBuilder.Entity<Post>(builder =>
            {
                builder.HasOne(p => p.Category).WithMany(c => c.Posts).HasForeignKey(p => p.CategoryId);

                builder.Property(p => p.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!)!,
                    new ValueComparer<List<string>>(
                        equalsExpression: (c1, c2) => c1!.SequenceEqual(c2!),
                        hashCodeExpression: c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        snapshotExpression: c => c.ToList()));
            });

            modelBuilder.Entity<Comment>(builder =>
            {
                builder.HasOne(c => c.User).WithMany(u => u.Comments).HasForeignKey(c => c.UserId);
                builder.HasOne(c => c.Post).WithMany(p => p.Comments).HasForeignKey(c => c.PostId);
                builder.HasOne(c => c.ParentComment).WithMany(c => c.ChildComments).HasForeignKey(c => c.ParentCommentId);
            });

            modelBuilder.Entity<User>(builder =>
            {
                builder.HasMany(u => u.BasicAccounts).WithOne(b => b.User).HasForeignKey(b => b.UserId);
                builder.HasMany(u => u.OAuthAccounts).WithOne(o => o.User).HasForeignKey(o => o.UserId);
                builder.HasMany(u => u.Roles).WithMany(r => r.Users);
            });

            ConfigureEntityBaseModels(modelBuilder);
        }

        private void ConfigureEntityBaseModels(ModelBuilder modelBuilder)
        {
            string defaultSoftDeletedAt = EntityBase.DefaultSoftDeletedAt.ToString("yyyy-MM-dd HH:mm:ss.ffffff");
            foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(t => t.ClrType.IsAssignableTo(typeof(EntityBase))))
            {
                if (entity.GetTableName() is not string tableName)
                {
                    continue;
                }

                IMutableProperty softDeletedAtProperty = entity.GetProperty(nameof(EntityBase.SoftDeletedAt));
                softDeletedAtProperty.SetDefaultValue(EntityBase.DefaultSoftDeletedAt);

                string softDeletedAtName = softDeletedAtProperty.GetColumnName();
                string softDeleteLevelName = entity.GetProperty(nameof(EntityBase.SoftDeleteLevel)).GetColumnName();

                string checkConstraintName = $"CK_{tableName}_{softDeleteLevelName}_{softDeletedAtName}";
                string checkConstraintSql = $"({softDeleteLevelName} = 0 AND {softDeletedAtName} = '{defaultSoftDeletedAt}') OR ({softDeleteLevelName} <> 0 AND {softDeletedAtName} <> '{defaultSoftDeletedAt}')";

                entity.AddCheckConstraint(checkConstraintName, checkConstraintSql);

                ParameterExpression parameterExp = Expression.Parameter(entity.ClrType);
                Expression softDeleteLevelProperty = Expression.Property(parameterExp, nameof(EntityBase.SoftDeleteLevel));
                Expression softDeleteLevelIsZeroExp = Expression.Equal(softDeleteLevelProperty, Expression.Constant((byte)0));
                LambdaExpression softDeleteLevelFilterLambda = Expression.Lambda(softDeleteLevelIsZeroExp, parameterExp);

                entity.SetQueryFilter(softDeleteLevelFilterLambda);

                var createdAtProperty = entity.GetProperty(nameof(EntityBase.CreatedAt));
                createdAtProperty.SetValueGenerationStrategy(MySqlValueGenerationStrategy.IdentityColumn);
                createdAtProperty.SetValueGeneratorFactory((_, _) => new DateTimeOffsetUtcNowGenerator(_timeProvider));
            }
        }
    }
}
