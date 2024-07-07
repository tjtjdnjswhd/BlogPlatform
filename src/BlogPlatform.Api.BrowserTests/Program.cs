using BlogPlatform.Api.BrowserTests;
using BlogPlatform.Api.BrowserTests.Options;
using BlogPlatform.Api.BrowserTests.Services;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddOptions<ApiUrls>().BindConfiguration("Api:Urls");
builder.Services.AddSingleton<CookieHandler>();

string baseAddress = builder.Configuration["Api:Urls:BaseAddress"] ?? throw new Exception();
builder.Services.AddSingleton<ApiClient>();
builder.Services.AddHttpClient<ApiClient>(client => client.BaseAddress = new(baseAddress)).AddDefaultLogger().AddHttpMessageHandler<CookieHandler>();

await builder.Build().RunAsync();
