using System;
using System.Threading.Tasks;

namespace Semtex.UT.ShouldPass.RedundantAsyncAwait;

public class Right
{
    public static Task<object> DoSomething(Func<Task<object>> something) 
    {
        return something();
    }
}