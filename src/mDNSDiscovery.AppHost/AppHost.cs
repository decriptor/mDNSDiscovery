var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.mDNSDiscovery_WebApp>("mdnswebapp");

builder.Build().Run();
