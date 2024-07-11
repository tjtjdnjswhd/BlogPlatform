using BlogPlatform.Api.BrowserTests.Options;
using BlogPlatform.Shared.Identity.Models;
using BlogPlatform.Shared.Models;

using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

using System.Net.Http.Json;
using System.Text.Json;

namespace BlogPlatform.Api.BrowserTests.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;

        public ApiUrls Urls { get; private set; }

        public ApiClient(HttpClient httpClient, IJSRuntime jsRuntime, IOptions<ApiUrls> urls)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            Urls = urls.Value;
        }

        public async Task<string?> GetUserInfoAsync()
        {
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Get, Urls.Identity.UserInfo);
            var response = await _httpClient.SendAsync(httpRequestMessage);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
        }

        public async Task BasicLoginAsync(BasicLoginInfo loginInfo, string? returnUrl)
        {
            string json = JsonSerializer.Serialize(loginInfo);
            await _jsRuntime.InvokeVoidAsync("ajaxRedirect", $"{Urls.Identity.BasicLogin}?returnurl={returnUrl}", "POST", json);
        }

        public async Task BasicSignUpAsync(BasicSignUpInfo signUpInfo, string? returnUrl)
        {
            string json = JsonSerializer.Serialize(signUpInfo);
            await _jsRuntime.InvokeVoidAsync("ajaxRedirect", $"{Urls.Identity.BasicSignUp}?returnurl={returnUrl}", "POST", json);
        }

        public async Task RemoveOAuthAsync(string provider)
        {
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Delete, string.Format(Urls.Identity.OAuthRemove, provider));
            var response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                await _jsRuntime.InvokeVoidAsync("alert", $"Status code: {response.StatusCode}. Header: {response.Headers} Content: {await response.Content.ReadAsStringAsync()}");
            }
        }

        public async Task SendVerifyEmailAsync(string email)
        {
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, Urls.Identity.SendVerifyEmail)
            {
                Content = JsonContent.Create(new { email })
            };

            var response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                await _jsRuntime.InvokeVoidAsync("alert", $"Status code: {response.StatusCode}. Header: {response.Headers} Content: {await response.Content.ReadAsStringAsync()}");
            }
        }

        public async Task ChangePasswordAsync(string newPassword)
        {
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, Urls.Identity.ChangePassword)
            {
                Content = JsonContent.Create(new PasswordModel(newPassword))
            };

            var response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                await _jsRuntime.InvokeVoidAsync("alert", $"Status code: {response.StatusCode}. Header: {response.Headers} Content: {await response.Content.ReadAsStringAsync()}");
            }
        }

        public async Task ChangeNameAsync(string newName)
        {
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, Urls.Identity.ChangeName)
            {
                Content = JsonContent.Create(new UserNameModel(newName))
            };

            var response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                await _jsRuntime.InvokeVoidAsync("alert", $"Status code: {response.StatusCode}. Header: {response.Headers} Content: {await response.Content.ReadAsStringAsync()}");
            }
        }

        public async Task SendChangeEmailCodeAsync(string newEmail)
        {
            HttpRequestMessage httpRequestMessage = new(HttpMethod.Post, Urls.Identity.ChangeEmail)
            {
                Content = JsonContent.Create(new EmailModel(newEmail))
            };

            var response = await _httpClient.SendAsync(httpRequestMessage);
            if (!response.IsSuccessStatusCode)
            {
                await _jsRuntime.InvokeVoidAsync("alert", $"Status code: {response.StatusCode}. Header: {response.Headers} Content: {await response.Content.ReadAsStringAsync()}");
            }
        }
    }
}