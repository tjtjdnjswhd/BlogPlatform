using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using Microsoft.JSInterop;

namespace BlogPlatform.Api.BrowserTests
{
    public class CookieHandler : DelegatingHandler
    {
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;

        public CookieHandler(NavigationManager navigationManager, IJSRuntime jsRuntime)
        {
            _navigationManager = navigationManager;
            _jsRuntime = jsRuntime;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            await _jsRuntime.InvokeVoidAsync("console.log", $"Status code: {response.StatusCode}. Header: {response.Headers} Content: {await response.Content.ReadAsStringAsync()}");

            if (response.Headers.Location is not null)
            {
                _navigationManager.NavigateTo(response.Headers.Location.ToString(), true);
            }
            return response;
        }
    }
}