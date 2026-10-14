using EventViewer.Infrastructure.Authentication;

namespace EventViewer.API.Authentication;

/// <summary>
/// Returns the bearer token of the current request so it can be forwarded to the events API
/// </summary>
public class ForwardedAccessTokenProvider : IAccessTokenProvider
{
    private const string BearerPrefix = "Bearer ";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardedAccessTokenProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var header = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        var token = header is not null && header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? header[BearerPrefix.Length..].Trim()
            : null;

        return Task.FromResult(string.IsNullOrEmpty(token) ? null : token);
    }
}
