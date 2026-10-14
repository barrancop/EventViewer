namespace EventViewer.Infrastructure.Authentication;

/// <summary>
/// Supplies the bearer token used to call the events API
/// </summary>
public interface IAccessTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
