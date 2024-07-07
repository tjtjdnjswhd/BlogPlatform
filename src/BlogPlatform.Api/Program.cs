using BlogPlatform.Api.Identity.Extensions;
using BlogPlatform.Api.Json;
using BlogPlatform.EFCore;
using BlogPlatform.Shared.Extensions;

using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(json =>
{
    json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    json.JsonSerializerOptions.Converters.Add(new JsonTimeSpanConverter());
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.MapType<TimeSpan>(() => new OpenApiSchema { Type = "string", Example = new OpenApiString("-d.hh:mm:ss.ffffff") });
});

builder.Services.AddDistributedMemoryCache();

IConfigurationSection optionsSection = builder.Configuration.GetRequiredSection("Options");
IConfigurationSection oauthProviderSection = builder.Configuration.GetRequiredSection("OAuthProviders");
builder.Services.AddIdentity(optionsSection, oauthProviderSection);

builder.Services.AddDbContext<BlogPlatformDbContext>(options =>
{
    options.UseMySql(builder.Configuration.GetConnectionString("BlogPlatform"), MySqlServerVersion.LatestSupportedServerVersion);
});

builder.Services.AddDbContext<BlogPlatformImgDbContext>(options =>
{
    options.UseMySql(builder.Configuration.GetConnectionString("BlogPlatformImg"), MySqlServerVersion.LatestSupportedServerVersion);
});

builder.Services.AddMailSender(optionsSection.GetRequiredSection("MailSender"));
builder.Services.AddPostImageService();

builder.Services.AddScoped<ICascadeSoftDeleteService, CascadeSoftDeleteService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors(b =>
    {
        b.AllowAnyHeader();
        b.AllowAnyMethod();
        b.WithOrigins("https://localhost:7169");
        b.AllowCredentials();
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }