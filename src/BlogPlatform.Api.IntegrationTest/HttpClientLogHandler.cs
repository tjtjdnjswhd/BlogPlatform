using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest
{
    public class HttpClientLogHandler : DelegatingHandler
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public HttpClientLogHandler(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SendAsync(request, cancellationToken).Result;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _testOutputHelper.WriteLine(request.ToString());
            if (request.Content is not null and not StreamContent)
            {
                if (request.Content is MultipartContent multipart)
                {
                    foreach (HttpContent content in multipart.Where(content => content is not StreamContent))
                    {
                        _testOutputHelper.WriteLine(await content.ReadAsStringAsync(cancellationToken));
                    }
                }
                else
                {
                    _testOutputHelper.WriteLine(await request.Content.ReadAsStringAsync(cancellationToken));
                }
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            _testOutputHelper.WriteLine(response.ToString());
            if (response.Content is not null && (!response.Content.Headers.ContentType?.MediaType?.StartsWith("image") ?? true))
            {
                if (response.Content is MultipartContent multipart)
                {
                    foreach (HttpContent content in multipart.Where(content => (!response.Content.Headers.ContentType?.MediaType?.StartsWith("image") ?? true)))
                    {
                        _testOutputHelper.WriteLine(await content.ReadAsStringAsync(cancellationToken));
                    }
                }
                else
                {
                    _testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync(cancellationToken));
                }
            }

            return response;
        }
    }
}
