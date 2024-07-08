using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace BlogPlatform.Api.BrowserTests
{
    public class CookieHandler : DelegatingHandler
    {
        private readonly NavigationManager _navigationManager;

        public CookieHandler(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            request.SetBrowserRequestOption("redirect", "manual");
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            if (response.Headers.Location is not null)
            {
                _navigationManager.NavigateTo(response.Headers.Location.ToString(), true);
            }

            return response;
        }
    }
}