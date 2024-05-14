﻿using BlogPlatform.EFCore.Internals;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using System.Text.Json;

namespace BlogPlatform.EFCore
{
    public class BlogPlatformDbContext : DbContext
    {
        public DbSet<Blog> Blogs { get; private set; }

        public DbSet<Category> Categories { get; private set; }

        public DbSet<Post> Posts { get; private set; }

        public DbSet<Comment> Comments { get; private set; }

        public DbSet<User> Users { get; private set; }

        public DbSet<BasicAccount> BasicAccounts { get; private set; }

        public DbSet<OAuthAccount> OAuthAccounts { get; private set; }

        public DbSet<OAuthProvider> OAuthProviders { get; private set; }

        public DbSet<Role> Roles { get; private set; }

        public BlogPlatformDbContext(DbContextOptions<BlogPlatformDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.AddInterceptors(new SoftDeleteInterceptor());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EntityBase>(builder =>
            {
                builder.UseTpcMappingStrategy();
                builder.HasQueryFilter(builder => builder.DeletedAt == null);

                builder.Property(e => e.CreatedAt).ValueGeneratedOnAdd().HasValueGenerator<DateTimeOffsetUtcNowGenerator>();
            });

            modelBuilder.Entity<Blog>(builder =>
            {
                builder.HasOne(b => b.User).WithOne(u => u.Blog).HasForeignKey<Blog>(b => b.UserId);
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
                builder.HasOne(u => u.BasicLoginAccount).WithOne(b => b.User).HasForeignKey<User>(u => u.BasicLoginAccountId);
                builder.HasMany(u => u.Roles).WithMany(r => r.Users);
            });

            modelBuilder.Entity<OAuthAccount>(builder =>
            {
                builder.HasOne(o => o.User).WithMany(u => u.OAuthAccounts).HasForeignKey(o => o.UserId);
            });
        }
    }
}
