using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;
using BlogPlatform.Shared.Identity.Models;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using System.Net;
using System.Runtime.CompilerServices;

using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest.Searchs
{
    public class SearchQueryTests : TestBase, ITestDataReset
    {
        public const string Identifier = "DbContextVerify";

        public SearchQueryTests(WebApplicationFactoryFixture applicationFactoryFixture, ITestOutputHelper testOutputHelper) : base(applicationFactoryFixture, testOutputHelper, "integration_searchQuery_test", ServiceLifetime.Singleton)
        {
        }

        public static readonly TheoryData<string> PostSearchQueries = new(
            "blogid=1",
            "blogid=1&title=searchTitle",
            "blogid=2&title=searchTitle&content=searchContent&createdatstart=2024-01-01&createdatend=2025-12-31",
            "blogid=3&title=searchTitle&content=searchContent&createdatstart=2024-01-01&createdatend=2025-12-31&tags=tag1&tags=tag2&tagfilteroption=all",
            "blogid=4&title=searchTitle&content=searchContent&createdatstart=2024-01-01&createdatend=2025-12-31&tags=tag1&tags=tag2&tagfilteroption=any",
            "blogid=5&title=searchTitle&content=searchContent&createdatstart=2024-01-01&createdatend=2025-12-31&orderby=createdAt&orderdirection=descending",
            "categoryId=1",
            "categoryid=1&title=searchTitle&content=searchContent&createdatestart=2024-01-01&createdatend=2025-12-31&orderby=createdAt&orderdirection=ascending",
            "categoryid=2&title=searchTitle&content=searchContent&createdatstart=2024-01-01&createdatend=2025-12-31&tags=tag1&tags=tag2&tagfilteroption=all&orderby=title&orderdirection=ascending",
            "categoryid=3&title=searchTitle&content=searchContent&createdatstart=2024-01-01&createdatend=2025-12-31&tags=tag1&tags=tag2&tagfilteroption=any&orderby=updatedat&orderdirection=ascending",
            "categoryid=4&title=searchTitle&content=searchContent&createdatstart=2024-01-01&createdatend=2025-12-31&tags=tag1&tags=tag2&tagfilteroption=any&orderby=createdat&orderdirection=ascending&page=22&pagesize=60"
        );

        [Theory]
        [MemberData(nameof(PostSearchQueries))]
        public async Task Post_Search_QueryVerify(string query)
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            Recording.Start(Identifier);

            // Act
            HttpResponseMessage response = await client.GetAsync($"/api/post/?{query}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            List<ToAppend> entries = [.. Recording.Stop(Identifier)];
            entries.Insert(0, new ToAppend("query", query));
            await Verify(entries).UseHashedParameters(query);
        }

        [Fact]
        public async Task Comment_ByPost_QueryVerify()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            Recording.Start(Identifier);

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/comment/post/1?page=22");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var entries = Recording.Stop(Identifier);
            await Verify(entries);
        }

        public static readonly TheoryData<string> CommentSearchQueries = new(
            "",
            "content=searchContent",
            "postid=1",
            "userid=1",
            "content=searchContent&postid=1&userid=1&page=22",
            "content=searchContent&postid=1&userid=1&orderby=CreatedAt&orderdirection=Descending",
            "content=searchContent&postid=1&userid=1&orderby=UpdatedAt&orderdirection=Ascending",
            "content=searchContent&postid=1&userid=1&orderby=Content&orderdirection=Descending",
            "content=searchContent&postid=1&userid=1&orderby=Post&orderdirection=Ascending",
            "content=searchContent&postid=1&userid=1&orderby=User&orderdirection=Descending",
            "content=searchContent&postid=1&userid=1&orderby=Post&orderdirection=Ascending&page=22"
        );

        [Theory]
        [MemberData(nameof(CommentSearchQueries))]
        public async Task Comment_Search_QueryVerify(string query)
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            Recording.Start(Identifier);

            // Act
            HttpResponseMessage response = await client.GetAsync($"/api/comment/?{query}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            List<ToAppend> entries = [.. Recording.Stop(Identifier)];
            entries.Insert(0, new ToAppend("query", query));
            await Verify(entries).UseHashedParameters(query);
        }

        public static readonly TheoryData<string> UserSearchQueries = new(
            "isremoved=false&name=username",
            "isremoved=true&name=username",
            "isremoved=false&email=email",
            "isremoved=true&email=email",
            "isremoved=false&id=userid"
        );

        [Theory]
        [MemberData(nameof(UserSearchQueries))]
        public async Task User_Search_QueryVerify(string query)
        {
            // Arrange
            using var scope = WebApplicationFactory.Services.CreateScope();

            User adminUser = Helper.GetFirstEntity<User>(WebApplicationFactory, u => u.Roles.Any(r => r.Name == "Admin"));
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, adminUser);

            HttpClient client = WebApplicationFactory.CreateLoggingClient(TestOutputHelper);
            Helper.SetAuthorizationHeader(client, authorizeToken);
            Recording.Start(Identifier);

            // Act
            HttpResponseMessage response = await client.GetAsync($"/api/admin/user/?{query}");

            // Assert
            List<ToAppend> entries = [.. Recording.Stop(Identifier)];
            entries.Insert(0, new ToAppend("query", query));
            await Verify(entries).UseHashedParameters(query);
        }

        protected override void InitWebApplicationFactory(IWebHostBuilder builder, string dbName)
        {
            base.InitWebApplicationFactory(builder, dbName);
            builder.ConfigureServices(services =>
            {
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();

                DbContextOptions<BlogPlatformDbContext> dbContextOptions = serviceProvider.GetRequiredService<DbContextOptions<BlogPlatformDbContext>>();
                DbContextOptionsBuilder<BlogPlatformDbContext> optionsBuilder = new(dbContextOptions);
                optionsBuilder.EnableRecording(Identifier);

                services.RemoveAll<DbContextOptions<BlogPlatformDbContext>>();
                services.AddSingleton(optionsBuilder.Options);
            });
        }

        protected override void SeedData()
        {
            ResetData();
        }

        public static void ResetData()
        {
            using var scope = FixtureByTestClassName[typeof(SearchQueryTests).Name].ApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = Helper.GetNotLoggingDbContext<BlogPlatformDbContext>(scope.ServiceProvider);

            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();

            User adminUser = new("admin", "admin@user.com");
            Role adminRole = new(PolicyConstants.AdminRolePolicy, 0);
            dbContext.Users.Add(adminUser);
            dbContext.Roles.Add(adminRole);
            adminUser.Roles = [adminRole];
            dbContext.SaveChanges();
        }

        [ModuleInitializer]
        internal static void Init()
        {
            DbContextOptionsBuilder<BlogPlatformDbContext> optionsBuilder = new();
            optionsBuilder.UseMySql("server=fake;database=fake", MySqlServerVersion.LatestSupportedServerVersion);
            BlogPlatformDbContext dbContext = new(optionsBuilder.Options);
            VerifyEntityFramework.Initialize(dbContext.Model);
        }
    }
}
