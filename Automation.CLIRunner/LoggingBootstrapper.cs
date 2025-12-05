using Microsoft.Extensions.Logging;
using Serilog;

namespace Automation.CliRunner;

internal static class LoggingBootstrapper
{
    public static ILoggerFactory InitLogging(LogLevel logLevel = LogLevel.Information)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(
                path:$"Automation-Cli-{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.log", 
                rollingInterval: RollingInterval.Day,
                shared:false,
                retainedFileCountLimit:10,
                rollOnFileSizeLimit: false)
            .MinimumLevel.Verbose()
            .CreateLogger();

        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(logger: Log.Logger, dispose: false);
        });

        return loggerFactory;
    }
}