using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Semtex.Logging;

internal static class SemtexLog
{
    // This is trash.
    public static ILoggerFactory LoggerFactory = null!;

    public static void InitializeLogging(LogLevel verbosity, bool shouldLogToFile, string logDirectory, bool simpleMode)
    {
        SemtexConsoleFormatter.SimpleMode = simpleMode;
        const string timestampFormat = "HH:mm:ss.fff";
        var logPath = $"{logDirectory}/{DateTime.Now:yyyy-M-d_HH-mm-ss}.txt";
        var logFolder = Directory.GetParent(logPath)!.ToString();
        if (!Directory.Exists(logFolder)) Directory.CreateDirectory(logFolder);
        LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("", verbosity)
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                .AddConsole(options =>
                {
                    options.FormatterName = nameof(SemtexConsoleFormatter);
                })
                .AddConsoleFormatter<SemtexConsoleFormatter, ConsoleFormatterOptions>(options =>
                {
                    options.TimestampFormat = timestampFormat;
                });
            if (shouldLogToFile)
                builder.AddProvider(new SemtexLoggingProvider(logPath, timestampFormat));
        });
    }

    /// <summary>
    /// Makes it easy to grep the logs
    /// </summary>
    /// <param name="Id"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string GetPerformanceStr(string Id, long value)
    {
        return $"Performance: {Id} took {value}ms";
    }
}