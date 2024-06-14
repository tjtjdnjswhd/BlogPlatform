using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Identity.Services;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Models;

using Meziantou.Extensions.Logging.Xunit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using Moq;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest.Identity
{
    public class IdentityTests
    {
        public const string ACCESS_TOKEN_NAME = "access_token";
        public const string REFRESH_TOKEN_NAME = "refresh_token";

        public WebApplicationFactory<Program> WebApplicationFactory { get; private set; }

        public ITestOutputHelper TestOutputHelper { get; }

        public IdentityTests(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
            XUnitLoggerProvider loggerProvider = new(testOutputHelper, new XUnitLoggerOptions() { IncludeCategory = true, IncludeLogLevel = true });
            WebApplicationFactory = new();
            WebApplicationFactory = WebApplicationFactory.WithWebHostBuilder(cnf =>
            {
                cnf.ConfigureLogging(lb =>
                {
                    lb.ClearProviders();
                    lb.AddProvider(loggerProvider);
                });

                cnf.ConfigureAppConfiguration(builder =>
                {
                    string settingPath = Path.Combine(Directory.GetCurrentDirectory(), "Identity", "appsettings.json");
                    builder.AddJsonFile(settingPath);
                });

                cnf.ConfigureServices(services =>
                {
                    services.AddOptions<JwtOptions>().Configure(options =>
                    {
                        options.AccessTokenName = ACCESS_TOKEN_NAME;
                        options.RefreshTokenName = REFRESH_TOKEN_NAME;
                    }).ValidateDataAnnotations().ValidateOnStart();

                    services.AddOptions<AccountOptions>().Configure(options =>
                    {
                        options.MinIdLength = 5;
                        options.MaxIdLength = 20;
                        options.MinNameLength = 3;
                        options.MaxNameLength = 50;
                        options.MinPasswordLength = 4;
                        options.MaxPasswordLength = int.MaxValue;
                    }).ValidateDataAnnotations().ValidateOnStart();

                    JsonNode connectionStringNode = JsonNode.Parse(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "testConnectionStrings.json"))) ?? throw new Exception();
                    string connectionString = connectionStringNode["BlogPlatformDb"]?.GetValue<string>() ?? throw new Exception();
                    connectionString += "database=auth_test_blogplatform;";

                    services.RemoveAll<DbContextOptions<BlogPlatformDbContext>>();
                    services.RemoveAll<BlogPlatformDbContext>();
                    services.RemoveAll<DbContextOptions>();

                    services.AddDbContext<BlogPlatformDbContext>(opt =>
                    {
                        opt.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion);
                        opt.EnableDetailedErrors();
                        opt.EnableSensitiveDataLogging();
                    });
                });
            });

            SeedData();
        }

        [Fact]
        public async Task BasicLogin_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", new BasicLoginInfo("notexist", "password"));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BasicLogin_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", new BasicLoginInfo("user1Id", "wrongpassword"));

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BasicLogin_Invalid_Id()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            BasicLoginInfo basicLoginInfo = new("abc", "password");

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", basicLoginInfo);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BasicLogin_Invalid_Password()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            BasicLoginInfo basicLoginInfo = new("user0Id", "abc");

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", basicLoginInfo);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BasicLogin_Ok_Body()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", new BasicLoginInfo("user1Id", "user1pw"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            AuthorizeToken? authorizeToken = await response.Content.ReadFromJsonAsync<AuthorizeToken>();
            Assert.NotNull(authorizeToken);
            response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieValues);
            Assert.Null(cookieValues);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BasicLogin_Ok_Cookie()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();
            client.DefaultRequestHeaders.Add(HeaderNameConstants.AuthorizeTokenSetCookie, "true");

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", new BasicLoginInfo("user1Id", "user1pw"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await Assert.ThrowsAsync<JsonException>(async () => await response.Content.ReadFromJsonAsync<AuthorizeToken>());
            response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieValues);
            Assert.NotNull(cookieValues);
            TestOutputHelper.WriteLine($"cookieValues:{cookieValues}");
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BasicSignUp_EmailVerifyRequired()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55", "user55pw", "user55", "user55@user.com"));

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Error? error = await response.Content.ReadFromJsonAsync<Error>();
            Assert.NotNull(error);
            Assert.Equal("이메일 인증 후 가입해야 합니다.", error.Message);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BasicSignUp_IdExist()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user1Id", "user55pw", "user55", "user55@user.com"));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Error? error = await response.Content.ReadFromJsonAsync<Error>();
            Assert.NotNull(error);
            Assert.Equal("중복된 Id입니다", error.Message);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BasicSignUp_NameExist()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", "user1", "user55@user.com"));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Error? error = await response.Content.ReadFromJsonAsync<Error>();
            Assert.NotNull(error);
            Assert.Equal("중복된 이름입니다", error.Message);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BasicSignUp_EmailExist()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", "user55", "user1@user.com"));

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Error? error = await response.Content.ReadFromJsonAsync<Error>();
            Assert.NotNull(error);
            Assert.Equal("중복된 이메일입니다", error.Message);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BasicSignUp_Success_Cookie()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            string email = "user55@user.com";
            IDistributedCache distributedCache = WebApplicationFactory.Services.GetRequiredService<IDistributedCache>();
            await distributedCache.SetStringAsync($"{UserEmailService.VerifiedEmailPrefix}_{email}", string.Empty);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", "user55", email));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            AuthorizeToken? authorizeToken = await response.Content.ReadFromJsonAsync<AuthorizeToken>();
            Assert.NotNull(authorizeToken);
            response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieValues);
            Assert.Null(cookieValues);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task BasicSignUp_Success_Body()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            string email = "user55@user.com";
            IDistributedCache distributedCache = WebApplicationFactory.Services.GetRequiredService<IDistributedCache>();
            await distributedCache.SetStringAsync($"{UserEmailService.VerifiedEmailPrefix}_{email}", string.Empty);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", "user55", email));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await Assert.ThrowsAsync<JsonException>(async () => await response.Content.ReadFromJsonAsync<AuthorizeToken>());
            response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieValues);
            Assert.NotNull(cookieValues);
            TestOutputHelper.WriteLine($"cookieValues:{cookieValues}");
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task SendVerifyEmail_Ok()
        {
            // Arrange
            Mock<IUserEmailService> userEmailServiceMock = new();
            userEmailServiceMock.Setup(s => s.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<Func<string, string>>(), It.IsAny<CancellationToken>()));
            SetIEmailServiceMock(userEmailServiceMock);
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic/email", new { email = "user@user.com" });

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task VerifyEmail_BadRequest()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/signup/basic/email?code=123456");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task VerifyEmail_Ok()
        {
            // Arrange
            Mock<IUserEmailService> userEmailServiceMock = new();
            userEmailServiceMock.Setup(s => s.VerifyEmailCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("success");
            SetIEmailServiceMock(userEmailServiceMock);
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/signup/basic/email?code=123456");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task OAuthLogin_Challenge()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions() { AllowAutoRedirect = false });

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/oauth", new { provider = "Google" });

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task OAuthSignUp_Challenge()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions() { AllowAutoRedirect = false });

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/oauth", new { provider = "Google" });

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task AddOAuth_Unauthorize()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/oauth?provider=google");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task AddOAuth_Challenge()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions() { AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "gvdasbjobvals");

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/oauth?provider=google");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            TestOutputHelper.WriteLine(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task RemoveOAuth_Unauthorize()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task RemoveOAuth_Conflict()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task RemoveOAuth_UserNotFound()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task RemoveOAuth_ProviderNotFound()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task RemoveOAuth_Ok()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Logout_Ok()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Logout_NoContent()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Refresh_NotFound()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Refresh_Ok()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Refresh_NoContent()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ChangePassword_Unauthorize()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ChangePassword_UserNotFound()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ChangePassword_Ok()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ResetPassword_NotFound()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ResetPassword_Ok()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ChangeName_UserNotFound()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ChangeName_Ok()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task FindId_NotFound()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task FindId_Ok()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task WithDraw_Unauthorize()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task WithDraw_UserNotFound()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task WithDraw_Ok()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task CancelWithDraw_UserNotFound()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task CancelWithDraw_Expired()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task CancelWithDraw_WithDrawNotRequested()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task CancelWithDraw_Unauthorize()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task CancelWithDraw_Ok()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ChangeEmail_Unauthorize()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ChangeEmail_Ok()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ConfirmChangeEmail_WrongCode()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ConfirmChangeEmail_UserNotFound()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task ConfirmChangeEmail_Ok()
        {
            throw new NotImplementedException();
        }

        private void SeedData()
        {
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            IPasswordHasher<BasicAccount> passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<BasicAccount>>();

            dbContext.Database.EnsureDeleted();
            dbContext.Database.Migrate();
            // Seed data
            List<User> users = [
                new User("user1", "user1@user.com"),
                new User("user2", "user2@user.com"),
                new User("admin1", "admin1@admin.com")
            ];

            dbContext.Users.AddRange(users);
            dbContext.SaveChanges();

            List<Role> roles = [
                new Role("User", 1),
                new Role("Admin", 0)
            ];

            dbContext.Roles.AddRange(roles);
            dbContext.SaveChanges();

            users[0].Roles.Add(roles[0]);
            users[1].Roles.Add(roles[0]);
            users[2].Roles.Add(roles[1]);

            dbContext.SaveChanges();

            List<BasicAccount> basicAccounts = [
                new BasicAccount("user1Id", passwordHasher.HashPassword(null, "user1pw"), users[0].Id),
                new BasicAccount("user2Id", passwordHasher.HashPassword(null, "user2pw"), users[1].Id),
                new BasicAccount("admin1Id", passwordHasher.HashPassword(null, "admin1pw"), users[2].Id)
            ];

            dbContext.BasicAccounts.AddRange(basicAccounts);
            dbContext.SaveChanges();
        }

        private void SetIEmailServiceMock(Mock<IUserEmailService> emailServiceMock)
        {
            WebApplicationFactory = WebApplicationFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IUserEmailService>();
                    services.AddSingleton(emailServiceMock.Object);
                });
            });
        }
    }
}
