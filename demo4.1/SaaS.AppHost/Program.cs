var builder = DistributedApplication.CreateBuilder(args);

var weatherApi = builder.AddProject<Projects.SaaS_Backend>("weatherapi")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development");

builder.AddProject<Projects.SaaS_Frontend>("frontend")
    .WithReference(weatherApi)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WithExternalHttpEndpoints();

builder.Build().Run();
