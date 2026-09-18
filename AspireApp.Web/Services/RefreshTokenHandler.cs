using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AspireApp.Web.Services;

public class RefreshTokenHandler : DelegatingHandler
{
    private readonly ApiAuthService _authService;
    private readonly IHttpClientFactory _httpFactory;

    public RefreshTokenHandler(ApiAuthService authService, IHttpClientFactory httpFactory)
    {
        _authService = authService;
        _httpFactory = httpFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Attach current token if present
        if (!string.IsNullOrEmpty(_authService.JwtToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.JwtToken);
        }

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            return response;

        // Try to refresh token
        var refreshed = await _authService.TryRefreshTokenAsync();
        if (!refreshed)
            return response; // still unauthorized

        // Retry original request with new token
        var cloned = await CloneHttpRequestMessageAsync(request);
        cloned.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.JwtToken);
        return await base.SendAsync(cloned, cancellationToken);
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri);

        // Copy the request's content (via a MemoryStream) into the cloned object
        if (req.Content != null)
        {
            var ms = new MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            // copy the content headers
            if (req.Content.Headers != null)
            {
                foreach (var h in req.Content.Headers)
                    clone.Content.Headers.Add(h.Key, h.Value);
            }
        }

        clone.Version = req.Version;

        foreach (var header in req.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var prop in req.Properties)
            clone.Properties.Add(prop);

        return clone;
    }
}
