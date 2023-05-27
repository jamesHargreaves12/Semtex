using Microsoft.Extensions.Logging;

namespace Semtex.Logging;


internal class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly string _category;
    private readonly string _timestampFormat;
    private static object _lock = new object();
    
    // Cheap and dirty way of turning off file logging in the tests.
    internal static bool ActuallyWriteToFile = true; 
    public FileLogger(string path, string category, string timestampFormat)
    {
        _filePath = path;
        _category = category;
        _timestampFormat = timestampFormat;
    }
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Information;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string>? formatter)
    {
        if (formatter == null) return;
        var message = formatter(state, exception);
        var logLine = SemtexLogFormatting.FormatLog(logLevel, _timestampFormat, _category, message);
        
        if(!ActuallyWriteToFile)
            return;

        lock (_lock)
        {
            File.AppendAllText(_filePath, logLine + "\n");
        }
    }
}
