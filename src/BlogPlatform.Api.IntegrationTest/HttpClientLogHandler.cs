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
            request.Content?.ReadAsStringAsync(cancellationToken).ContinueWith(task =>
            {
                _testOutputHelper.WriteLine(task.Result);
            }, cancellationToken);
            _testOutputHelper.WriteLine(request.ToString());

            HttpResponseMessage response = base.Send(request, cancellationToken);

            _testOutputHelper.WriteLine(response.ToString());
            response.Content?.ReadAsStringAsync(cancellationToken).ContinueWith(task =>
            {
                _testOutputHelper.WriteLine(task.Result);
            }, cancellationToken);

            return response;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _testOutputHelper.WriteLine(request.ToString());
            if (request.Content != null)
            {
                _testOutputHelper.WriteLine(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            _testOutputHelper.WriteLine(response.ToString());
            if (response.Content != null)
            {
                _testOutputHelper.WriteLine(await response.Content.ReadAsStringAsync(cancellationToken));
            }

            return response;
        }
    }
}
