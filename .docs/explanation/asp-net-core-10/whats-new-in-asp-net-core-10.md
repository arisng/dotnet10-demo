# What's New in ASP.NET Core 10

This document explains the key features and enhancements introduced in ASP.NET Core 10 as part of .NET 10, providing context on why these changes matter for modern web development.

## Why ASP.NET Core 10 Matters

ASP.NET Core 10 addresses real-world challenges in building resilient web applications, particularly around network reliability, JavaScript interoperability, and API documentation. It builds on the foundation of previous versions while introducing improvements that enhance performance, security, and developer productivity.

## Blazor Enhancements

Blazor continues to evolve as a powerful framework for client-side web experiences. The updates in ASP.NET Core 10 focus on reliability, performance, and user experience.

### Automatic Fingerprinting and Asset Optimization

Previously, Blazor scripts were embedded resources, but now they're treated as static web assets with built-in compression and fingerprinting. This improves caching efficiency and security against tampering. For standalone WebAssembly apps, enable client-side fingerprinting by adding `<OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>` to your `.csproj` file.

### Reconnection UI and State Persistence

Network interruptions are now handled gracefully with the new `ReconnectModal` component and declarative state persistence using the `[PersistentState]` attribute. This automatically saves and restores data across prerendering, navigation, or disconnections, eliminating the need for manual JSON serialization.

### JavaScript Interop Improvements

JavaScript interop now supports async methods on `IJSRuntime` and `IJSObjectReference`, including `InvokeConstructorAsync` and `GetValueAsync<T>`, with `CancellationToken` support for timeouts. This simplifies complex JS manipulations and adds AOT compatibility for trimmed deployments.

## Minimal APIs Advancements

Minimal APIs in .NET 10 prioritize simplicity and robustness, making them more suitable for microservices and rapid prototyping.

### Built-in Validation with DataAnnotations

Use `builder.Services.AddValidation()` for automatic validation of parameters via attributes. On failure, endpoints return 400 Bad Request with customizable responses through `IProblemDetailsService`. This applies to query, header, body parameters, and collections.

### Server-Sent Events (SSE) Support

Real-time streaming is now supported using `TypedResults.ServerSentEvents`, automating JSON formatting and avoiding common mistakes. This is ideal for dashboards, live metrics, or push notifications without WebSockets.

### Form Binding Enhancements

Empty form strings now map to null for nullable types (like `DateOnly?`), preventing parse errors. This makes Minimal APIs more approachable for complex objects.

## OpenAPI Improvements

ASP.NET Core 10 offers enhanced documentation options with OpenAPI 3.1 compliance, including YAML support for cleaner syntax. Enable XML comments for richer descriptions, and benefit from improved schema handling for enums, JSON Patch, and invariant culture formatting.

## Performance and Diagnostics

Observability gets a boost with new metrics for Blazor circuits, navigation tracing, and WebAssembly profiling tools. Enable with `builder.Services.AddMetrics()` for automatic instrumentation. Preloaded assets and inlined boot configurations reduce startup times.

## Breaking Changes and Migration

Upgrading requires awareness of changes like `NavLinkMatch.All` ignoring query strings, HttpClient streaming enabled by default, and the deprecation of `<NotFound>` in favor of `NotFoundPage`. Other changes include cookie login redirects for API endpoints and deprecations of certain packages.

## Comparison with Previous Versions

ASP.NET Core 10 builds significantly on 8/9, adding features like automatic asset optimization, state persistence, built-in validation, SSE support, and OpenAPI 3.1 compliance. It also introduces better metrics, profiling tools, and form handling improvements.

For more details, refer to the [official ASP.NET Core 10 release notes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0).

## Related Documentation

- [How-to: Upgrade to ASP.NET Core 10](../how-to/upgrade-to-asp-net-core-10.md)
- [Reference: ASP.NET Core 10 API Changes](../reference/asp-net-core-10-api-changes.md)
- [Tutorials: Building Your First Blazor App with ASP.NET Core 10](../../tutorials/getting-started-with-blazor-10.md)
