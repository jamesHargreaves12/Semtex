namespace Semtex.UT.ShouldPass.LazyInitialization;

public class Right
{
    private Wrapper? _lazyWrapper;

    public Wrapper GetWrapper()
    {
        if (_lazyWrapper == null)
        {
            _lazyWrapper = new Wrapper();
        }
    
        return _lazyWrapper;
    }
    
    public void Log()
    {
        if (_lazyWrapper == null)
        {
            _lazyWrapper = new Wrapper(1);
        }
        _lazyWrapper.Log();
    }

}