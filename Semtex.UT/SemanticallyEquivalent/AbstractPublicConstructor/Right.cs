using System;

namespace Semtex.UT.ShouldPass.AbstractPublicConstructor;

public class Right
{
    public abstract class MyClass
    {
        public MyClass()
        {
            Console.WriteLine("You can only access this by creating a child.");
        }
    }
}