var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent);

var db = postgres.AddDatabase("skillcert");

// S3-compatible blob store for local dev (RustFS). The hosted demo uses DigitalOcean Spaces.
var s3AccessKey = builder.AddParameter("s3-access-key");
var s3SecretKey = builder.AddParameter("s3-secret-key", secret: true);

var s3 = builder.AddContainer("s3", "rustfs/rustfs", "1.0.1")
    .WithEnvironment("RUSTFS_ACCESS_KEY", s3AccessKey)
    .WithEnvironment("RUSTFS_SECRET_KEY", s3SecretKey)
    .WithEnvironment("RUSTFS_CONSOLE_ENABLE", "true")
    .WithHttpEndpoint(targetPort: 9000, name: "api")
    .WithHttpEndpoint(targetPort: 9001, name: "console")
    .WithVolume("skillcert-s3-data", "/data")
    .WithHttpHealthCheck("/health", endpointName: "api")
    .WithLifetime(ContainerLifetime.Persistent);

// Fake login accounts for local dev; every account shares this password.
var demoPassword = builder.AddParameter("demo-password", secret: true);

var migrator = builder.AddProject<Projects.SkillCert_Migrator>("migrator")
    .WithReference(db)
    .WithEnvironment("Seed__DemoUsers", "true")
    .WithEnvironment("Seed__DemoPassword", demoPassword)
    .WaitFor(db);

var api = builder.AddProject<Projects.SkillCert_Api>("api")
    .WithReference(db)
    .WithEnvironment("S3__ServiceUrl", s3.GetEndpoint("api"))
    .WithEnvironment("S3__AccessKey", s3AccessKey)
    .WithEnvironment("S3__SecretKey", s3SecretKey)
    .WithEnvironment("S3__Bucket", "training-records")
    .WaitFor(db)
    .WaitFor(s3)
    .WaitForCompletion(migrator)
    .WithHttpHealthCheck("/health");

// Vite dev server; proxies /api to the API so the auth cookie stays same-origin.
builder.AddViteApp("web", "../../web")
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
