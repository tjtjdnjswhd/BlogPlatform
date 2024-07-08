using BlogPlatform.Api.BrowserTests;
using BlogPlatform.Api.BrowserTests.Options;
using BlogPlatform.Api.BrowserTests.Services;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddOptions<ApiUrls>().BindConfiguration("API:Urls");
builder.Services.AddSingleton<CookieHandler>();

builder.Services.AddSingleton<ApiClient>();
builder.Services.AddHttpClient<ApiClient>(client => client.BaseAddress = new(builder.HostEnvironment.BaseAddress)).AddDefaultLogger().AddHttpMessageHandler<CookieHandler>();

await builder.Build().RunAsync();
