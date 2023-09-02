using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Semtex.Logging;

public sealed class SemtexConsoleFormatter : ConsoleFormatter, IDisposable
{
    private readonly IDisposable? _optionsReloadToken;
    private ConsoleFormatterOptions _formatterOptions;
    public static bool SimpleMode = false;
    public SemtexConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> options)
        // Case insensitive
        : base(nameof(SemtexConsoleFormatter)) => (_optionsReloadToken, _formatterOptions) =
        (options.OnChange(ReloadLoggerOptions), options.CurrentValue);
    private void ReloadLoggerOptions(ConsoleFormatterOptions options) => _formatterOptions = options;

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter.Invoke(logEntry.State, logEntry.Exception);
        var text = SimpleMode
            ? SemtexLogFormatting.FormatLogSimple(logEntry.LogLevel, _formatterOptions.TimestampFormat, message)
            : SemtexLogFormatting.FormatLog(logEntry.LogLevel, _formatterOptions.TimestampFormat, logEntry.Category, message);

        textWriter.WriteLine(text);
    }

    public void Dispose() => _optionsReloadToken?.Dispose();
}