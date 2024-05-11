﻿using BlogPlatform.EFCore;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using MySqlConnector;

var builder = Host.CreateApplicationBuilder(args);

MySqlConnectionStringBuilder mySqlConnectionStringBuilder = builder.Configuration.GetRequiredSection("MySqlConnectionString").Get<MySqlConnectionStringBuilder>() ?? throw new Exception();

builder.Services.AddDbContext<BlogPlatformDbContext>(options =>
{
    options.UseMySql(mySqlConnectionStringBuilder.ToString(), new MySqlServerVersion(new Version(8, 0, 36)));
});

var app = builder.Build();

app.Run();