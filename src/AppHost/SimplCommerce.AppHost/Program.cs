var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql", port: 11433)
    .WithDataVolume("simplcommerce-sqldata")
    .WithLifetime(ContainerLifetime.Persistent);

var simplDb = sql.AddDatabase("SimplCommerce");

var redis = builder.AddRedis("redis")
    .WithRedisCommander();

var storage = builder.AddAzureStorage("storage").RunAsEmulator();
var blobs = storage.AddBlobs("blobs");

var mail = builder.AddMailPit("mail");

var seq = builder.AddSeq("seq")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var api = builder.AddProject<Projects.SimplCommerce_ApiService>("api")
    .WithReference(simplDb)
    .WithReference(redis)
    .WithReference(blobs)
    .WithReference(mail)
    .WithReference(seq)
    .WaitFor(sql);

builder.AddProject<Projects.SimplCommerce_Storefront>("storefront")
    .WithReference(api)
    .WithReference(redis)
    .WithReference(seq)
    .WaitFor(api);

builder.AddProject<Projects.SimplCommerce_Admin>("admin")
    .WithReference(api)
    .WithReference(redis)
    .WithReference(seq)
    .WaitFor(api);

builder.AddProject<Projects.SimplCommerce_WebHost>("webhost")
    .WithReference(simplDb)
    .WithReference(redis)
    .WithReference(blobs)
    .WithReference(mail)
    .WithReference(seq)
    .WithReference(api)
    .WaitFor(sql);

builder.Build().Run();
