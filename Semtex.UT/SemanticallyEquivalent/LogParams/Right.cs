using Microsoft.Extensions.Logging;

namespace Semtex.UT.SemanticallyEquivalent.LogParams;

public class Right
{
    private Logger<Wrapper> _logger;

    public Right(Logger<Wrapper> logger)
    {
        _logger = logger;
    }

    public void Main()
    {
        _logger.LogInformation("Something {Y}", 1);
    }

}