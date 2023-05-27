using Microsoft.Extensions.Logging;

namespace Semtex.Logging;

internal sealed class SemtexLoggingProvider: ILoggerProvider
{
    private readonly string _path;
    private readonly string _timestampFormat;

    public SemtexLoggingProvider(string path, string timestampFormat)
    {
        _path = path;
        _timestampFormat = timestampFormat;
    }
    
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(_path, categoryName, _timestampFormat);
    }

    public void Dispose()
    {
    }
}