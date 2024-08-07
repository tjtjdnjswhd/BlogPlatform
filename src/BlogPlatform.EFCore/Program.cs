﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MySqlConnector;

namespace BlogPlatform.EFCore;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton(TimeProvider.System);

        MySqlConnectionStringBuilder mySqlConnectionStringBuilder = builder.Configuration.GetRequiredSection("MySqlConnectionString").Get<MySqlConnectionStringBuilder>() ?? throw new Exception();

        builder.Services.AddDbContext<BlogPlatformDbContext>(options =>
        {
            options.UseMySql(mySqlConnectionStringBuilder.ToString(), new MySqlServerVersion(new Version(8, 0, 36)));
        });

        builder.Services.AddDbContext<BlogPlatformImgDbContext>(options =>
        {
            options.UseMySql(mySqlConnectionStringBuilder.ToString(), new MySqlServerVersion(new Version(8, 0, 36)));
        });

        var app = builder.Build();

        app.Run();
    }
}