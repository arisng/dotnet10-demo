var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.Demo5_1_ApiService>("apiservice");

builder.AddProject<Projects.Demo5_1_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService);

builder.Build().Run();
