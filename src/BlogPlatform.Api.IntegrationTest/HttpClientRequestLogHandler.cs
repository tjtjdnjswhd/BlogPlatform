using Xunit.Abstractions;

namespace BlogPlatform.Api.IntegrationTest
{
    public class HttpClientRequestLogHandler : DelegatingHandler
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public HttpClientRequestLogHandler(ITestOutputHelper testOutputHelper)
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
            return base.Send(request, cancellationToken);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Content?.ReadAsStringAsync(cancellationToken).ContinueWith(task =>
            {
                _testOutputHelper.WriteLine(task.Result);
            }, cancellationToken);
            _testOutputHelper.WriteLine(request.ToString());
            return base.SendAsync(request, cancellationToken);
        }
    }
}
