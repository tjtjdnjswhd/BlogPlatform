using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Extensions;
using BlogPlatform.Api.Json;
using BlogPlatform.Api.Swagger;
using BlogPlatform.Shared.Extensions;

using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.Services.AddRazorPages();
#endif

builder.Services.AddControllers().AddJsonOptions(json =>
{
    json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    json.JsonSerializerOptions.Converters.Add(new JsonTimeSpanConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddTimeSpanSchema();
    options.AddBearerAuthorization();
});

builder.Services.AddDistributedMemoryCache();

IConfigurationSection optionsSection = builder.Configuration.GetRequiredSection("Options");
IConfigurationSection oauthProviderSection = builder.Configuration.GetRequiredSection("OAuthProviders");

builder.Services.AddIdentity(optionsSection, oauthProviderSection);
builder.Services.AddDbServices(builder.Configuration);

builder.Services.AddMailSender(optionsSection.GetRequiredSection("MailSender"));
builder.Services.AddPostImageService();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

#if DEBUG
app.UseWebAssemblyDebugging();
#endif

app.UseHttpsRedirection();

#if DEBUG
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
#endif

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

#if DEBUG
app.MapRazorPages();
app.MapFallbackToFile("index.html");
#endif

app.Run();

public partial class Program { }