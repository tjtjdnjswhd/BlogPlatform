﻿// <auto-generated />
using System;
using BlogPlatform.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BlogPlatform.EFCore.Migrations
{
    [DbContext(typeof(BlogPlatformDbContext))]
    partial class BlogPlatformDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.4")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Abstractions.EntityBase", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<DateTimeOffset>("CreatedAt"));

                    b.Property<DateTimeOffset>("DeletedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime(6)")
                        .HasDefaultValue(new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

                    b.Property<DateTime>("Version")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp(6)");

                    b.HasKey("Id");

                    b.ToTable((string)null);

                    b.UseTpcMappingStrategy();
                });

            modelBuilder.Entity("RoleUser", b =>
                {
                    b.Property<int>("RolesId")
                        .HasColumnType("int");

                    b.Property<int>("UsersId")
                        .HasColumnType("int");

                    b.HasKey("RolesId", "UsersId");

                    b.HasIndex("UsersId");

                    b.ToTable("RoleUser");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.BasicAccount", b =>
                {
                    b.HasBaseType("BlogPlatform.EFCore.Models.Abstractions.EntityBase");

                    b.Property<string>("AccountId")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasIndex("AccountId", "DeletedAt")
                        .IsUnique();

                    b.ToTable("BasicAccounts");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Blog", b =>
                {
                    b.HasBaseType("BlogPlatform.EFCore.Models.Abstractions.EntityBase");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasIndex("UserId")
                        .IsUnique();

                    b.HasIndex("Name", "DeletedAt")
                        .IsUnique();

                    b.HasIndex("UserId", "DeletedAt")
                        .IsUnique();

                    b.ToTable("Blog");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Category", b =>
                {
                    b.HasBaseType("BlogPlatform.EFCore.Models.Abstractions.EntityBase");

                    b.Property<int>("BlogId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasIndex("BlogId");

                    b.ToTable("Category");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Comment", b =>
                {
                    b.HasBaseType("BlogPlatform.EFCore.Models.Abstractions.EntityBase");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("LastUpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<int?>("ParentCommentId")
                        .HasColumnType("int");

                    b.Property<int>("PostId")
                        .HasColumnType("int");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasIndex("ParentCommentId");

                    b.HasIndex("PostId");

                    b.HasIndex("UserId");

                    b.ToTable("Comment");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.OAuthAccount", b =>
                {
                    b.HasBaseType("BlogPlatform.EFCore.Models.Abstractions.EntityBase");

                    b.Property<string>("NameIdentifier")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("ProviderId")
                        .HasColumnType("int");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasIndex("ProviderId");

                    b.HasIndex("UserId");

                    b.HasIndex("NameIdentifier", "ProviderId", "DeletedAt")
                        .IsUnique();

                    b.ToTable("OAuthAccount");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.OAuthProvider", b =>
                {
                    b.HasBaseType("BlogPlatform.EFCore.Models.Abstractions.EntityBase");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.HasIndex("Name", "DeletedAt")
                        .IsUnique();

                    b.ToTable("OAuthProvider");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Post", b =>
                {
                    b.HasBaseType("BlogPlatform.EFCore.Models.Abstractions.EntityBase");

                    b.Property<int>("CategoryId")
                        .HasColumnType("int");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTimeOffset>("LastUpdatedAt")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Tags")
                        .IsRequired()
                        .HasColumnType("json");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasIndex("CategoryId");

                    b.ToTable("Post");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Role", b =>
                {
                    b.HasBaseType("BlogPlatform.EFCore.Models.Abstractions.EntityBase");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<int>("Priority")
                        .HasColumnType("int");

                    b.HasIndex("Name", "DeletedAt")
                        .IsUnique();

                    b.HasIndex("Priority", "DeletedAt")
                        .IsUnique();

                    b.ToTable("Role");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.User", b =>
                {
                    b.HasBaseType("BlogPlatform.EFCore.Models.Abstractions.EntityBase");

                    b.Property<int?>("BasicLoginAccountId")
                        .HasColumnType("int");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("varchar(255)");

                    b.HasIndex("BasicLoginAccountId")
                        .IsUnique();

                    b.HasIndex("Email", "DeletedAt")
                        .IsUnique();

                    b.HasIndex("UserName", "DeletedAt")
                        .IsUnique();

                    b.ToTable("User");
                });

            modelBuilder.Entity("RoleUser", b =>
                {
                    b.HasOne("BlogPlatform.EFCore.Models.Role", null)
                        .WithMany()
                        .HasForeignKey("RolesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlogPlatform.EFCore.Models.User", null)
                        .WithMany()
                        .HasForeignKey("UsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Blog", b =>
                {
                    b.HasOne("BlogPlatform.EFCore.Models.User", "User")
                        .WithOne("Blog")
                        .HasForeignKey("BlogPlatform.EFCore.Models.Blog", "UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Category", b =>
                {
                    b.HasOne("BlogPlatform.EFCore.Models.Blog", "Blog")
                        .WithMany("Categories")
                        .HasForeignKey("BlogId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Blog");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Comment", b =>
                {
                    b.HasOne("BlogPlatform.EFCore.Models.Comment", "ParentComment")
                        .WithMany("ChildComments")
                        .HasForeignKey("ParentCommentId");

                    b.HasOne("BlogPlatform.EFCore.Models.Post", "Post")
                        .WithMany("Comments")
                        .HasForeignKey("PostId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlogPlatform.EFCore.Models.User", "User")
                        .WithMany("Comments")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ParentComment");

                    b.Navigation("Post");

                    b.Navigation("User");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.OAuthAccount", b =>
                {
                    b.HasOne("BlogPlatform.EFCore.Models.OAuthProvider", "Provider")
                        .WithMany()
                        .HasForeignKey("ProviderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BlogPlatform.EFCore.Models.User", "User")
                        .WithMany("OAuthAccounts")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Provider");

                    b.Navigation("User");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Post", b =>
                {
                    b.HasOne("BlogPlatform.EFCore.Models.Category", "Category")
                        .WithMany("Posts")
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Category");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.User", b =>
                {
                    b.HasOne("BlogPlatform.EFCore.Models.BasicAccount", "BasicLoginAccount")
                        .WithOne("User")
                        .HasForeignKey("BlogPlatform.EFCore.Models.User", "BasicLoginAccountId");

                    b.Navigation("BasicLoginAccount");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.BasicAccount", b =>
                {
                    b.Navigation("User")
                        .IsRequired();
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Blog", b =>
                {
                    b.Navigation("Categories");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Category", b =>
                {
                    b.Navigation("Posts");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Comment", b =>
                {
                    b.Navigation("ChildComments");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.Post", b =>
                {
                    b.Navigation("Comments");
                });

            modelBuilder.Entity("BlogPlatform.EFCore.Models.User", b =>
                {
                    b.Navigation("Blog");

                    b.Navigation("Comments");

                    b.Navigation("OAuthAccounts");
                });
#pragma warning restore 612, 618
        }
    }
}
