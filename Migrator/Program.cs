using Core;
using Serilog;
using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Migrator;

class Program
{
    public static IConfigurationRoot Configuration { get; private set; }
    public static string LogWriteFilePath = string.Empty;
    public static readonly bool IsDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

    private static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.AddSerilog();
    });
    private static readonly ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

    static int Main(string[] args)
    {
        ConfigureLoggingBootstrap();

        try
        {
            ConfigureAppConfiguration(args);
            ConfigureLogging();

            var serviceProvider = CreateServices();

            using var scope = serviceProvider.CreateScope();
            UpdateDatabase(scope.ServiceProvider);

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "DbMigration terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static void ConfigureAppConfiguration(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"secrets/appsettings.{CommonConstants.EnvironmentName}.secret.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        if (IsDevelopment)
        {
            builder.AddUserSecrets<Program>();
        }
        Configuration = builder.Build();
        if (!string.IsNullOrEmpty(LogWriteFilePath))
        {
            Log.Logger.Information($"LogFilePath: {LogWriteFilePath}");
        }
    }

    private static void ConfigureLoggingBootstrap()
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: CommonConstants.LOG_OUTPUT_TEMPLATE)
            .CreateLogger();
    }

    private static void ConfigureLogging()
    {
        var logFilePath = Configuration["DbMigration:LogFilePath"];
        var loggerConfig = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: CommonConstants.LOG_OUTPUT_TEMPLATE);
        if (!string.IsNullOrEmpty(logFilePath))
        {
            var ext = Path.GetExtension(logFilePath);
            LogWriteFilePath = Path.ChangeExtension(logFilePath, $"[{DateTime.Now:dd-MM-yy HH-mm-ss}]{ext}");
            loggerConfig.WriteTo.File(LogWriteFilePath, outputTemplate: CommonConstants.LOG_OUTPUT_TEMPLATE, rollingInterval: RollingInterval.Infinite);
        }
        Log.Logger = loggerConfig.CreateLogger();
    }

    /// <summary>
    /// Configure the dependency injection services
    /// </summary>
    private static IServiceProvider CreateServices()
    {
        if (!int.TryParse(Configuration["DbMigration:SqlCommandTimeoutSec"], out var timeout))
        {
            timeout = 1800;
        }
        var sqlCommandTimeout = TimeSpan.FromSeconds(timeout);
        return new ServiceCollection()
            .AddFluentMigratorCore()
            .AddLogging(lb => lb.AddFluentMigratorConsole().AddSerilog(Log.Logger))
            .ConfigureRunner(rb => rb
                // Add SqlServer support to FluentMigrator
                .AddPostgres()
                .WithGlobalConnectionString(Configuration.GetConnectionString("DefaultConnection"))
                .WithGlobalCommandTimeout(sqlCommandTimeout)
                .ScanIn(typeof(Program).Assembly).For.Migrations().For.EmbeddedResources())
            .BuildServiceProvider(false);
    }

    /// <summary>
    /// Update the database
    /// </summary>
    private static void UpdateDatabase(IServiceProvider serviceProvider)
    {
        if (Configuration.GetConnectionString("DefaultConnection") == null)
        {
            throw new NullReferenceException("appsettings.DefaultConnection");
        }
        // Instantiate the runner
        var runner = serviceProvider.GetRequiredService<IMigrationRunner>();
        Log.Information("--------- Migration started ---------");

        // Execute the migrations
        runner.MigrateUp();
        Log.Information("--------- Migration finished ---------");
    }
}