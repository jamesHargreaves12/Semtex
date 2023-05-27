namespace Semtex.UT.ShouldPass.LazyInitialization;

public class Left
{
    private Wrapper? _lazyWrapper;

    public Wrapper GetWrapper()
    {
        return _lazyWrapper ??= new Wrapper();
    }
    
    public void Log()
    {
        (_lazyWrapper ??= new Wrapper(1)).Log();
    }
}