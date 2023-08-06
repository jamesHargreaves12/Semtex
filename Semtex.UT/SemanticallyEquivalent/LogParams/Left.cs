using Microsoft.Extensions.Logging;

namespace Semtex.UT.SemanticallyEquivalent.LogParams;

public class Left
{
    private Logger<Wrapper> _logger;

    public Left(Logger<Wrapper> logger)
    {
        _logger = logger;
    }

    public void Main()
    {
        _logger.LogInformation("Something {X}", 1);
    }
}