
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace BlogPlatform.Api.BrowserTests
{
    public class CookieHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            request.SetBrowserRequestMode(BrowserRequestMode.Cors);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
