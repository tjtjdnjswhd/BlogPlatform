namespace BlogPlatform.Api.Helper
{
    public static class HttpRequestExtensions
    {
        public static string GetBaseUri(this HttpRequest request)
        {
            UriBuilder uriBuilder = new(request.Scheme, request.Host.Host, request.Host.Port ?? -1);
            if (uriBuilder.Uri.IsDefaultPort)
            {
                uriBuilder.Port = -1;
            }

            return uriBuilder.Uri.AbsoluteUri;
        }
    }
}
