namespace SaaS.Shared;

public interface IGraphProfileService
{
    Task<GraphUserProfile?> GetMyProfileAsync(CancellationToken cancellationToken = default);
}
