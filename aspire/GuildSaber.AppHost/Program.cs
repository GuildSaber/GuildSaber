using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var mySqlConnectionString = builder.AddConnectionString("mysqldb");
    
builder.AddProject<GuildSaber_Api>("api")
    .WithReference(mySqlConnectionString);

builder.Build().Run();