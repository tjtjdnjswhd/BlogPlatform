using BlogPlatform.Api.Identity.Constants;
using BlogPlatform.Api.Identity.Models;
using BlogPlatform.Api.Identity.Options;
using BlogPlatform.Api.Identity.Services.Interfaces;
using BlogPlatform.Api.Models;
using BlogPlatform.EFCore;
using BlogPlatform.EFCore.Extensions;
using BlogPlatform.EFCore.Models;
using BlogPlatform.EFCore.Models.Abstractions;

using Meziantou.Extensions.Logging.Xunit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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
                        options.AccessTokenName = Helper.ACCESS_TOKEN_NAME;
                        options.RefreshTokenName = Helper.REFRESH_TOKEN_NAME;
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

                    services.AddDistributedMemoryCache();
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
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task BasicLogin_Unauthorized()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", new BasicLoginInfo("user1Id", "wrongpassword"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task BasicLogin_Ok_Body()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/basic", new BasicLoginInfo("user1Id", "user1pw"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            AuthorizeToken? authorizeToken = await response.Content.ReadFromJsonAsync<AuthorizeToken>();
            Assert.NotNull(authorizeToken);
            response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieValues);
            Assert.Null(cookieValues);
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
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await Assert.ThrowsAsync<JsonException>(async () => await response.Content.ReadFromJsonAsync<AuthorizeToken>());
            response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieValues);
            Assert.NotNull(cookieValues);
            TestOutputHelper.WriteLine($"cookieValues:{cookieValues}");
        }

        [Fact]
        public async Task BasicSignUp_EmailVerifyRequired()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55", "user55pw", "user55", "user55@user.com"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task BasicSignUp_IdExist()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();
            User user = Helper.GetFirstUser(WebApplicationFactory);
            string email = "user55@user.com";
            Helper.SetVerifiedEmail(WebApplicationFactory, email);
            Helper.LoadCollection(WebApplicationFactory, user, u => u.BasicAccounts);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo(user.BasicAccounts.First().AccountId, "user55pw", "user55", email));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Error? error = await response.Content.ReadFromJsonAsync<Error>();
            Assert.NotNull(error);
            Assert.Equal("중복된 Id입니다", error.Message);
        }

        [Fact]
        public async Task BasicSignUp_NameExist()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();
            User user = Helper.GetFirstUser(WebApplicationFactory);
            string email = "user55@user.com";
            Helper.SetVerifiedEmail(WebApplicationFactory, email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", user.Name, email));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Error? error = await response.Content.ReadFromJsonAsync<Error>();
            Assert.NotNull(error);
            Assert.Equal("중복된 이름입니다", error.Message);
        }

        [Fact]
        public async Task BasicSignUp_EmailExist()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();
            User user = Helper.GetFirstUser(WebApplicationFactory);
            Helper.SetVerifiedEmail(WebApplicationFactory, user.Email);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", "user55", user.Email));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Error? error = await response.Content.ReadFromJsonAsync<Error>();
            Assert.NotNull(error);
            Assert.Equal("중복된 이메일입니다", error.Message);
        }

        [Fact]
        public async Task BasicSignUp_Success_Cookie()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            string email = "user55@user.com";
            Helper.SetVerifiedEmail(WebApplicationFactory, email);
            client.DefaultRequestHeaders.Add(HeaderNameConstants.AuthorizeTokenSetCookie, "true");

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", "user55", email));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await Assert.ThrowsAnyAsync<JsonException>(async () => await response.Content.ReadFromJsonAsync<AuthorizeToken>());
            response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieValues);
            Assert.NotNull(cookieValues);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BasicSignUp_Success_Body(bool setCookieSetToken)
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            string email = "user55@user.com";
            Helper.SetVerifiedEmail(WebApplicationFactory, email);
            if (setCookieSetToken)
            {
                client.DefaultRequestHeaders.Add(HeaderNameConstants.AuthorizeTokenSetCookie, "false");
            }

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic", new BasicSignUpInfo("user55Id", "user55pw", "user55", email));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            AuthorizeToken? authorizeToken = await response.Content.ReadFromJsonAsync<AuthorizeToken>();
            Assert.NotNull(authorizeToken);
            response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookieValues);
            Assert.Null(cookieValues);
            TestOutputHelper.WriteLine($"cookieValues:{cookieValues}");
        }

        [Fact]
        public async Task SendVerifyEmail_Ok()
        {
            // Arrange
            SetIEmailServiceMock();
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/basic/email", new EmailModel("user@user.com"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task VerifyEmail_BadRequest()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/signup/basic/email?code=123456");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task OAuthLogin_Challenge()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions() { AllowAutoRedirect = false });

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/login/oauth", new Models.OAuthProvider("Google"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task OAuthSignUp_Challenge()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions() { AllowAutoRedirect = false });

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/signup/oauth", new Models.OAuthProvider("Google"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task AddOAuth_Unauthorize()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/oauth?provider=google");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddOAuth_Challenge_Header()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions() { AllowAutoRedirect = false });
            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/oauth?provider=google");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task AddOAuth_Challenge_Cookie()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient(new WebApplicationFactoryClientOptions() { AllowAutoRedirect = false });
            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizeTokenCookie(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/identity/oauth?provider=google");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task RemoveOAuth_Unauthorize()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/identity/oauth/google");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RemoveOAuth_Conflict()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User oauthUser = new("oauthOnly", "oauthOnly@user.com");
            dbContext.Users.Add(oauthUser);
            dbContext.SaveChanges();

            OAuthAccount oAuthAccount = new("googleNameIdentifier", 1, oauthUser.Id);
            dbContext.OAuthAccounts.Add(oAuthAccount);
            dbContext.SaveChanges();

            oauthUser.Roles.Add(dbContext.Roles.Where(r => r.Name == "user").First());
            dbContext.SaveChanges();

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, oauthUser);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/identity/oauth/google");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.True(dbContext.OAuthAccounts.Any(o => o.Id == oAuthAccount.Id));
        }

        [Fact]
        public async Task RemoveOAuth_UserNotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User oauthUser = new("oauthOnly", "oauthOnly@user.com");
            dbContext.Users.Add(oauthUser);
            dbContext.SaveChanges();

            OAuthAccount oAuthAccount = new("googleNameIdentifier", 1, oauthUser.Id);
            dbContext.OAuthAccounts.Add(oAuthAccount);
            dbContext.SaveChanges();

            oauthUser.Roles.Add(dbContext.Roles.Where(r => r.Name == "user").First());
            dbContext.SaveChanges();

            Helper.SoftDelete(WebApplicationFactory, oauthUser, TestOutputHelper);

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, oauthUser);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/identity/oauth/google");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.True(dbContext.OAuthAccounts.Any(o => o.Id == oAuthAccount.Id));
        }

        [Fact]
        public async Task RemoveOAuth_ProviderNotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User oauthUser = new("newUser", "newUser@user.com");
            dbContext.Users.Add(oauthUser);
            dbContext.SaveChanges();

            OAuthAccount oAuthAccount = new("google", 1, oauthUser.Id);
            dbContext.OAuthAccounts.Add(oAuthAccount);
            dbContext.SaveChanges();

            oauthUser.Roles.Add(dbContext.Roles.Where(r => r.Name == "user").First());
            dbContext.SaveChanges();

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, oauthUser);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/identity/oauth/notexist");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.True(dbContext.OAuthAccounts.Any(o => o.Id == oAuthAccount.Id));
        }

        [Fact]
        public async Task RemoveOAuth_OAuthOnly_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User oauthUser = new("newUser", "newUser@user.com");
            dbContext.Users.Add(oauthUser);
            dbContext.SaveChanges();

            OAuthAccount googleAccount = new("googleNameIdentifier", 1, oauthUser.Id);
            OAuthAccount naverAccount = new("naverNameIdentifier", 2, oauthUser.Id);

            dbContext.OAuthAccounts.AddRange(googleAccount, naverAccount);
            dbContext.SaveChanges();

            oauthUser.Roles.Add(dbContext.Roles.Where(r => r.Name == "user").First());
            dbContext.SaveChanges();

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, oauthUser);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/identity/oauth/google");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(dbContext.OAuthAccounts.IgnoreSoftDeleteFilter().Any(o => o.Id == googleAccount.Id));
            Assert.False(dbContext.OAuthAccounts.Any(o => o.Id == googleAccount.Id));
            Assert.True(dbContext.OAuthAccounts.Any(o => o.Id == naverAccount.Id));
        }

        [Fact]
        public async Task RemoveOAuth_OneOAuthWithBasic_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();
            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User oauthUser = new("newUser", "newUser@user.com");
            dbContext.Users.Add(oauthUser);
            dbContext.SaveChanges();

            OAuthAccount googleAccount = new("googleNameIdentifier", 1, oauthUser.Id);
            BasicAccount basicAccount = new("newUser", "newUser", oauthUser.Id);

            dbContext.OAuthAccounts.Add(googleAccount);
            dbContext.BasicAccounts.Add(basicAccount);
            dbContext.SaveChanges();

            oauthUser.Roles.Add(dbContext.Roles.Where(r => r.Name == "user").First());
            dbContext.SaveChanges();

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, oauthUser);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.DeleteAsync("/api/identity/oauth/google");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(dbContext.OAuthAccounts.Any(o => o.Id == googleAccount.Id));
            Assert.True(dbContext.OAuthAccounts.IgnoreSoftDeleteFilter().Any(o => o.Id == googleAccount.Id));
            Assert.True(dbContext.BasicAccounts.Any(b => b.Id == basicAccount.Id));
        }

        [Fact]
        public async Task Logout_NoContent()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();
            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/logout", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Logout_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();
            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizeTokenCookie(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/logout", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/refresh", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();
            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            await Helper.SetAuthorizeTokenCache(WebApplicationFactory, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/refresh", authorizeToken, CancellationToken.None);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_TokenExpired()
        {
            // Arrange
            WebApplicationFactory = WebApplicationFactory.WithWebHostBuilder(conf =>
            {
                conf.ConfigureServices(services =>
                {
                    services.AddScoped<TimeProvider, FakeTimeProvider>(_ => new FakeTimeProvider(DateTimeOffset.UtcNow.AddDays(1)));
                });
            });

            HttpClient client = WebApplicationFactory.CreateClient();
            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            await Helper.SetAuthorizeTokenCache(WebApplicationFactory, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/refresh", authorizeToken, CancellationToken.None);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_Unauthorize()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/password/change", new PasswordModel("newPassword"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_UserNotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);
            Helper.SoftDelete(WebApplicationFactory, user, TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/password/change", new PasswordModel("newPassword"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_InvalidPassword()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User user = dbContext.Users.First();
            string oldPasswordHash = dbContext.BasicAccounts.First(b => b.UserId == user.Id).PasswordHash;

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/password/change", new PasswordModel("abc"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(oldPasswordHash, dbContext.BasicAccounts.First(b => b.UserId == user.Id).PasswordHash);
        }

        [Fact]
        public async Task ChangePassword_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            using var scope = WebApplicationFactory.Services.CreateScope();
            BlogPlatformDbContext dbContext = scope.ServiceProvider.GetRequiredService<BlogPlatformDbContext>();
            User user = dbContext.Users.First();
            string oldPasswordHash = dbContext.BasicAccounts.First(b => b.UserId == user.Id).PasswordHash;

            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/password/change", new PasswordModel("newPassword"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotEqual(oldPasswordHash, dbContext.BasicAccounts.First(b => b.UserId == user.Id).PasswordHash);
        }

        [Fact]
        public async Task ResetPassword_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/password/reset", new EmailModel("notExist@notExist"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_Ok()
        {
            // Arrange
            SetIEmailServiceMock();
            HttpClient client = WebApplicationFactory.CreateClient();
            User user = Helper.GetFirstUser(WebApplicationFactory);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/password/reset", new EmailModel(user.Email));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ChangeName_UserNotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);
            Helper.SoftDelete(WebApplicationFactory, user, TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/name", new UserNameModel("newName"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Changename_InvalidName()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/name", new UserNameModel("ab"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangeName_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/name", new UserNameModel("newName"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task FindId_NotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/id/find", new EmailModel("notExist@user.com"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task FindId_Ok()
        {
            // Arrange
            SetIEmailServiceMock(new Mock<IUserEmailService>());
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/identity/id/find", new EmailModel("user1@user.com"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task WithDraw_Unauthorize()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/withdraw", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task WithDraw_UserNotFound()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);
            Helper.SoftDelete(WebApplicationFactory, user, TestOutputHelper);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/withdraw", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task WithDraw_Ok()
        {
            // Arrange
            HttpClient client = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(client, authorizeToken);

            // Act
            HttpResponseMessage response = await client.PostAsync("/api/identity/withdraw", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CancelWithDraw_UserNotFound()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);
            Helper.HardDelete(WebApplicationFactory, user);

            // Act
            HttpResponseMessage response = await httpClient.PostAsync("/api/identity/withdraw/cancel", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CancelWithDraw_Expired()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);
            Helper.SoftDelete(WebApplicationFactory, user, TestOutputHelper);

            // Act
            HttpResponseMessage response = await httpClient.PostAsync("/api/identity/withdraw/cancel", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CancelWithDraw_WithDrawNotRequested()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PostAsync("/api/identity/withdraw/cancel", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CancelWithDraw_Unauthorize()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await httpClient.PostAsync("/api/identity/withdraw/cancel", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CancelWithDraw_Ok()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken); ;
            Helper.SoftDelete(WebApplicationFactory, user, TestOutputHelper);

            // Act
            HttpResponseMessage response = await httpClient.PostAsync("/api/identity/withdraw/cancel", null);

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Helper.ReloadEntity(WebApplicationFactory, user);
            Assert.Equal(0, user.SoftDeleteLevel);
            Assert.Equal(EntityBase.DefaultSoftDeletedAt, user.SoftDeletedAt);
        }

        [Fact]
        public async Task ChangeEmail_Unauthorize()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateClient();

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/identity/email/change", new EmailModel("newEmail@email.com"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangeEmail_Ok()
        {
            // Arrange
            SetIEmailServiceMock();
            HttpClient httpClient = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.PostAsJsonAsync("/api/identity/email/change", new EmailModel("newEmail@email.com"));

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmChangeEmail_WrongCode()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/identity/email/change/confirm?code=wrongCode");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmChangeEmail_UserNotFound()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);
            Helper.SoftDelete(WebApplicationFactory, user, TestOutputHelper);

            string newEmail = "newEmail@email.com";
            Helper.SetEmailVerifyCode(WebApplicationFactory, "code", newEmail);

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/identity/email/change/confirm?code=code'");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmChangeEmail_Ok()
        {
            // Arrange
            HttpClient httpClient = WebApplicationFactory.CreateClient();

            User user = Helper.GetFirstUser(WebApplicationFactory);
            AuthorizeToken authorizeToken = await Helper.GetAuthorizeTokenAsync(WebApplicationFactory, user);
            Helper.SetAuthorizationHeader(httpClient, authorizeToken);

            string newEmail = "newEmail@email.com";
            Helper.SetEmailVerifyCode(WebApplicationFactory, "code", newEmail);

            // Act
            HttpResponseMessage response = await httpClient.GetAsync("/api/identity/email/change/confirm?code=code'");

            // Assert
            PrintResponse(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Helper.ReloadEntity(WebApplicationFactory, user);
            Assert.Equal(newEmail, user.Email);
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

            List<EFCore.Models.OAuthProvider> oAuthProviders = [
                new EFCore.Models.OAuthProvider("Google"),
                new EFCore.Models.OAuthProvider("Naver")
            ];

            dbContext.OAuthProviders.AddRange(oAuthProviders);
            dbContext.SaveChanges();
        }

        private void SetIEmailServiceMock(Mock<IUserEmailService>? emailServiceMock = null)
        {
            WebApplicationFactory = WebApplicationFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IUserEmailService>();
                    if (emailServiceMock is null)
                    {
                        emailServiceMock = new();
                    }
                    services.AddSingleton(emailServiceMock.Object);
                });
            });
        }

        private void PrintResponse(HttpResponseMessage response)
        {
            TestOutputHelper.WriteLine($"Content: {response.Content.ReadAsStringAsync().Result}");
            TestOutputHelper.WriteLine($"Headers: {response.Headers}");
        }
    }
}
