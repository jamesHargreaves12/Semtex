using System;
using System.Threading.Tasks;

namespace Semtex.UT.ShouldPass.RedundantAsyncAwait;

public class Left
{
    public static async Task<object> DoSomething(Func<Task<object>> something) 
    {
        return await something().ConfigureAwait(false);
    }

}