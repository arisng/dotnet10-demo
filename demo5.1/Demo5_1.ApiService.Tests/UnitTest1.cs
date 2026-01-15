using Demo5_1.ApiService.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Xunit;

namespace Demo5_1.ApiService.Tests;

public class AuthorizationTests
{
    [Fact]
    public void ScopeRequirement_CreatesCorrectRequirement()
    {
        // Arrange & Act
        var requirement = new ScopeRequirement("access_as_user");

        // Assert
        Assert.Equal("access_as_user", requirement.Scope);
    }

    [Fact]
    public void PermissionRequirement_CreatesCorrectRequirement()
    {
        // Arrange & Act
        var requirement = new PermissionRequirement("weather.read");

        // Assert
        Assert.Equal("weather.read", requirement.Permission);
    }

    [Fact]
    public async Task ScopeAuthorizationHandler_Succeeds_WhenScopePresent()
    {
        // Arrange
        var handler = new ScopeAuthorizationHandler();
        var context = new AuthorizationHandlerContext(
            new[] { new ScopeRequirement("access_as_user") },
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("scp", "access_as_user profile")
            })),
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task ScopeAuthorizationHandler_Fails_WhenScopeMissing()
    {
        // Arrange
        var handler = new ScopeAuthorizationHandler();
        var context = new AuthorizationHandlerContext(
            new[] { new ScopeRequirement("access_as_user") },
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("scp", "profile email")
            })),
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_Succeeds_WhenPermissionPresent()
    {
        // Arrange
        var handler = new PermissionAuthorizationHandler();
        var context = new AuthorizationHandlerContext(
            new[] { new PermissionRequirement("weather.read") },
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("permission", "weather.read"),
                new Claim("permission", "users.write")
            })),
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_Fails_WhenPermissionMissing()
    {
        // Arrange
        var handler = new PermissionAuthorizationHandler();
        var context = new AuthorizationHandlerContext(
            new[] { new PermissionRequirement("reports.export") },
            new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("permission", "weather.read"),
                new Claim("permission", "users.write")
            })),
            null);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}
