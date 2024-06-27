using System.Net.Http.Json;

namespace BlogPlatform.Api.BrowserTests.Services
{
    public class ApiClient
    {
        private string? _baseAddress;

        public string BaseAddress
        {
            get => _baseAddress ?? string.Empty;
            set
            {
                _baseAddress = value;
                NotifyStateChanged();
            }
        }

        public string OAuthSignUpUri => BaseAddress + "/api/identity/signup/oauth";

        public string OAuthLoginUri => BaseAddress + "/api/identity/login/oauth";

        public string OAuthAddUri => BaseAddress + "/api/identity/oauth";

        public string EmailSendUri => BaseAddress + "/api/admin/send-email";

        public event Action? OnChange;

        public async Task BasicSignUp(string id, string password, string name, string email, bool setCookie)
        {
            HttpClient httpClient = new() { BaseAddress = new(BaseAddress) };
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, "/api/identity/signup/basic")
            {
                Content = JsonContent.Create(new { id, password, name, email })
            };

            if (setCookie)
            {
                httpRequestMessage.Headers.Add("token-set-cookie", "true");
            }

            var response = await httpClient.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
        }

        public async Task BasicLoginAsync(string id, string password, bool setCookie)
        {
            HttpClient httpClient = new() { BaseAddress = new(BaseAddress) };
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, "/api/identity/login/basic")
            {
                Content = JsonContent.Create(new { id, password })
            };

            if (setCookie)
            {
                httpRequestMessage.Headers.Add("token-set-cookie", "true");
            }

            var response = await httpClient.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();
        }

        public async Task LogoutAsync()
        {
            HttpClient httpClient = new() { BaseAddress = new(BaseAddress) };
            var response = await httpClient.PostAsync("/api/identity/logout", null);
            response.EnsureSuccessStatusCode();
        }

        public async Task SendEmailAsync(string subject, string body, List<int>? userIds)
        {
            HttpClient httpClient = new() { BaseAddress = new(BaseAddress) };
            HttpRequestMessage request = new(HttpMethod.Post, EmailSendUri)
            {
                Content = JsonContent.Create(new { Subject = subject, Body = body, UserIds = userIds })
            };

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
