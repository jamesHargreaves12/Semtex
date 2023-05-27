using System;

namespace Semtex.UT.ShouldPass.AbstractPublicConstructor;

public class Left
{
    public abstract class MyClass
    {
        protected MyClass()
        {
            Console.WriteLine("You can only access this by creating a child.");
        }
    }
}