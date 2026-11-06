using System.Reflection;
using DbUp;

var connectionString = args.Length > 0
    ? args[0]
    : Environment.GetEnvironmentVariable("ConnectionStrings__gighaus")
      ?? "Host=localhost;Port=5432;Database=gighaus;Username=postgres;Password=postgres";

Console.WriteLine($"Running migrations against: {connectionString[..connectionString.IndexOf("Password", StringComparison.OrdinalIgnoreCase)]}...");

EnsureDatabase.For.PostgresqlDatabase(connectionString);

var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(result.Error);
    Console.ResetColor();
    return -1;
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Database migration completed successfully.");
Console.ResetColor();
return 0;
