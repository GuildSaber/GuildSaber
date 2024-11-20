using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var mySqlConnectionString = builder.AddConnectionString("mysqldb");

builder.AddProject<GuildSaber_Api>("api")
    .WithReference(mySqlConnectionString)
    .WithHttpsEndpoint()
    .WithEnvironment(x => x.EnvironmentVariables.Add("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName));
//.WithHttpsHealthCheck("/health"); Crashes the app in current nuget package version

builder.Build().Run();