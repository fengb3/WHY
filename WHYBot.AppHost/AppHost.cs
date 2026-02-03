var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.WHYBot_Database>("whybot-database");

builder.Build().Run();
