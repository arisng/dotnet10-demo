using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DProcess.Idp;

public sealed class OpenIddictSeeder : IHostedService
{
    private readonly IServiceProvider serviceProvider;

    public OpenIddictSeeder(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        const string clientId = "bff";

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = "bff-secret",
            Type = ClientTypes.Confidential,
            DisplayName = "Blazor BFF",
            ConsentType = ConsentTypes.Implicit,
            RedirectUris =
            {
                new Uri("https://localhost:7092/signin-oidc")
            },
            PostLogoutRedirectUris =
            {
                new Uri("https://localhost:7092/signout-callback-oidc")
            },
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                "ept:userinfo",

                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                "rsp:form_post",

                "scp:openid",
                "scp:profile",
                "scp:email",
                "scp:offline_access",
                "scp:api"
            },
            Requirements =
            {
                Requirements.Features.ProofKeyForCodeExchange
            }
        };

        var application = await manager.FindByClientIdAsync(clientId, cancellationToken);
        if (application is not null)
        {
            // Delete existing application to ensure clean state
            await manager.DeleteAsync(application, cancellationToken);
        }
        
        // Create new application with correct descriptor
        await manager.CreateAsync(descriptor, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
