var builder = DistributedApplication.CreateBuilder(args);

var idp = builder.AddProject<Projects.DProcess_Idp>("idp")
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
	.WithEnvironment("DOTNET_ENVIRONMENT", "Development")
	.WithExternalHttpEndpoints();

var api = builder.AddProject<Projects.DProcess_Api>("api")
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
	.WithEnvironment("DOTNET_ENVIRONMENT", "Development")
	.WithEnvironment("Idp__Authority", "https://localhost:7046")
	.WithEnvironment("Idp__Issuer", "https://localhost:7046");

builder.AddProject<Projects.DProcess_Bff>("bff")
	.WithReference(api)
	.WithReference(idp)
	.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
	.WithEnvironment("DOTNET_ENVIRONMENT", "Development")
	.WithEnvironment("Idp__Authority", "https://localhost:7046")
	.WithEnvironment("ReverseProxy__Clusters__api__Destinations__d1__Address", "https://localhost:7142/")
	.WithExternalHttpEndpoints();

builder.Build().Run();
