using BlogPlatform.Api.Identity.Extensions;
using BlogPlatform.Api.Options;
using BlogPlatform.Api.Services;
using BlogPlatform.Api.Services.Interfaces;
using BlogPlatform.EFCore;

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddScoped<IMailSender, MailSender>();
builder.Services.Configure<MailSenderOptions>(optionsSection.GetRequiredSection("MailSender"));

builder.Services.AddScoped<IPostImageService, PostImageService>();

builder.Services.AddScoped<ICascadeSoftDeleteService, CascadeSoftDeleteService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.UseAuthentication();

app.MapControllers();

app.Run();

public partial class Program { }