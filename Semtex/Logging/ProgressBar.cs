using System.Text;
using Microsoft.Extensions.Logging;

namespace Semtex.Logging;

internal class ProgressBar<T>
{
    private readonly double _total;
    private int _prevChars = -1;
    private readonly ILogger<T> _logger;
    private const int BAR_SIZE = 50;

    public ProgressBar(int total, ILogger<T> logger)
    {
        _total = total;
        _logger = logger;
    }

    public void Update(double progress)
    {
        // Draw the progress bar
        var pct = progress / _total;
        var chars = (int)Math.Floor(pct * BAR_SIZE);
        if(chars == _prevChars)
            return;
        
        var builder = new StringBuilder();
        builder.Append("[");
        for (var i = 0; i < BAR_SIZE; i++)
        {
            builder.Append(i < chars ? "#" : " ");
        }

        builder.Append("]");
        builder.Append($" ({pct * 100:0.00}%)");
        _prevChars = chars;
        _logger.LogInformation(builder.ToString());
    }

}