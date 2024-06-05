using BlogPlatform.EFCore;

using Microsoft.EntityFrameworkCore;

namespace BlogPlatform.Api.Tests
{
    public class DbContextMySqlMigrateFixture : IDisposable
    {
        public Dictionary<string, DbContextOptions<BlogPlatformDbContext>> OptionsByDBName { get; private set; } = [];

        public DbContextOptions<BlogPlatformDbContext> Init(string dbName)
        {
            if (OptionsByDBName.TryGetValue(dbName, out DbContextOptions<BlogPlatformDbContext>? value))
            {
                return value;
            }

            // https://learn.microsoft.com/en-us/visualstudio/test/configure-unit-tests-by-using-a-dot-runsettings-file
            DbContextOptionsBuilder<BlogPlatformDbContext> optionsBuilder = new();
            string mySqlHost = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? throw new NullReferenceException(".RunSettings 파일 필요");
            string mySqlId = Environment.GetEnvironmentVariable("MYSQL_ID") ?? throw new NullReferenceException(".RunSettings 파일 필요");
            string mySqlPassword = Environment.GetEnvironmentVariable("MYSQL_PW") ?? throw new NullReferenceException(".RunSettings 파일 필요");

            optionsBuilder.UseMySql($"Server={mySqlHost};Uid={mySqlId};Pwd={mySqlPassword};database={dbName};", new MySqlServerVersion(MySqlServerVersion.LatestSupportedServerVersion));
            OptionsByDBName.Add(dbName, optionsBuilder.Options);
            using BlogPlatformDbContext dbContext = new(optionsBuilder.Options);
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
            return optionsBuilder.Options;
        }

        public void Dispose()
        {
            foreach (var options in OptionsByDBName.Values)
            {
                using BlogPlatformDbContext dbContext = new(options);
                dbContext.Database.EnsureDeleted();
            }
        }
    }
}
