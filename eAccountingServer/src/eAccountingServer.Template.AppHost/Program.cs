var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.eAccountingServer_WebApi>("eaccountingserver-webapi");

builder.Build().Run();
