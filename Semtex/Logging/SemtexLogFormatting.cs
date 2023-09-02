using Microsoft.Extensions.Logging;

namespace Semtex.Logging;

public static class SemtexLogFormatting
{
    public static string FormatLog(
        LogLevel logLevel,
        string? timestampFormat,
        string? category,
        string message)
    {
        return $"{DateTime.Now.ToString(timestampFormat)} [{GetLogLevelString(logLevel)}] {category} {message}";
    }

    public static string FormatLogSimple(
        LogLevel logLevel,
        string? timestampFormat,
        string message)
    {
        if (logLevel == LogLevel.Information)
            return $"{DateTime.Now.ToString(timestampFormat)} {message}";

        return $"{DateTime.Now.ToString(timestampFormat)} [{GetLogLevelString(logLevel)}] {message}";
    }

    // Copied from https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Logging.Console/src/SimpleConsoleFormatter.cs
    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "trce",
            LogLevel.Debug => "dbug",
            LogLevel.Information => "info",
            LogLevel.Warning => "warn",
            LogLevel.Error => "fail",
            LogLevel.Critical => "crit",
            _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
        };
    }

}