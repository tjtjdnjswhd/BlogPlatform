using BlogPlatform.Api.Helper;
using BlogPlatform.Api.Identity.Extensions;
using BlogPlatform.Api.Json;
using BlogPlatform.Api.Swagger;
using BlogPlatform.Shared.Extensions;

using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddRazorPages();
}

builder.Services.AddProblemDetails();

builder.Services.AddControllers().AddJsonOptions(json =>
{
    json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    json.JsonSerializerOptions.Converters.Add(new JsonTimeSpanConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    options.EnableAnnotations();
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
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapRazorPages();
    app.MapFallbackToFile("index.html");
}

app.Run();

public partial class Program { }