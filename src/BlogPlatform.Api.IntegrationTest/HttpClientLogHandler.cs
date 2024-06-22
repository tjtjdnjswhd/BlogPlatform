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
            _testOutputHelper.WriteLine(request.ToString());
            if (request.Content is not null and not StreamContent)
            {
                if (request.Content is MultipartContent multipart)
                {
                    foreach (HttpContent content in multipart.Where(content => content is not StreamContent))
                    {
                        _testOutputHelper.WriteLine(content.ReadAsStringAsync(cancellationToken).Result);
                    }
                }
                else
                {
                    _testOutputHelper.WriteLine(request.Content.ReadAsStringAsync(cancellationToken).Result);
                }
            }

            HttpResponseMessage response = base.Send(request, cancellationToken);

            _testOutputHelper.WriteLine(response.ToString());
            if (response.Content is not null and not StreamContent)
            {
                if (response.Content is MultipartContent multipart)
                {
                    foreach (HttpContent content in multipart.Where(content => content is not StreamContent))
                    {
                        _testOutputHelper.WriteLine(content.ReadAsStringAsync(cancellationToken).Result);
                    }
                }
                else
                {
                    _testOutputHelper.WriteLine(response.Content.ReadAsStringAsync(cancellationToken).Result);
                }
            }
            return response;
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
            if (response.Content is not null and not StreamContent)
            {
                if (response.Content is MultipartContent multipart)
                {
                    foreach (HttpContent content in multipart.Where(content => content is not StreamContent))
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
