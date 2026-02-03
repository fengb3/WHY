var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.WHY_Api>("why-api");

builder.Build().Run();
