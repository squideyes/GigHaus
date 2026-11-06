var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin()
    .AddDatabase("gighaus");

var redis = builder.AddRedis("redis");

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

// MailDev — local SMTP + web UI for email testing
var maildev = builder.AddContainer("maildev", "maildev/maildev")
    .WithHttpEndpoint(port: 1080, targetPort: 1080, name: "webui")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");

// Database migrations (run before API/Web start)
var migrations = builder.AddProject<Projects.GigHaus_Migrations>("migrations")
    .WithReference(postgres)
    .WaitFor(postgres);

// Admin account, created on startup when none exists yet.
// Set locally with: dotnet user-secrets set Parameters:admin-password <value>
var adminEmail = builder.AddParameter("admin-email", "admin@gighaus.local");
var adminPassword = builder.AddParameter("admin-password", secret: true);

// Web API
var webapi = builder.AddProject<Projects.GigHaus_WebApi>("web-api")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(maildev.GetEndpoint("smtp"))
    .WaitFor(postgres)
    .WaitFor(rabbitmq)
    .WaitForCompletion(migrations);

// Blazor site: marketing, host portal and admin
builder.AddProject<Projects.GigHaus_Web>("web-admin")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(maildev.GetEndpoint("smtp"))
    .WithReference(webapi)
    .WithEnvironment("Admin__Email", adminEmail)
    .WithEnvironment("Admin__Password", adminPassword)
    .WaitFor(postgres)
    .WaitForCompletion(migrations);

builder.Build().Run();
