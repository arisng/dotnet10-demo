namespace SaaS.Frontend.Services;

public interface IGraphService
{
    Task<GraphUserProfile?> GetMyProfileAsync(CancellationToken cancellationToken = default);
}

