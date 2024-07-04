using System.Text;

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
            if (request.Content?.Headers.ContentLength < 10000)
            {
                Stream requestContentStream = request.Content.ReadAsStream(cancellationToken);
                StreamReader streamReader = new(requestContentStream, Encoding.UTF8);
                _testOutputHelper.WriteLine(await streamReader.ReadToEndAsync(cancellationToken));
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            _testOutputHelper.WriteLine(response.ToString());

            if (response.Content.Headers.ContentLength < 10000)
            {
                Stream responseContentStream = response.Content.ReadAsStream(cancellationToken);
                StreamReader streamReader = new(responseContentStream, Encoding.UTF8);
                _testOutputHelper.WriteLine(await streamReader.ReadToEndAsync(cancellationToken));
            }
            return response;
        }
    }
}
