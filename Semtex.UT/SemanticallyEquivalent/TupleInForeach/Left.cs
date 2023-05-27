using System;
using System.Collections.Generic;

namespace Semtex.UT.SemanticallyEquivalent.TupleInForeach;

public class Left
{
    public static void Main(Dictionary<string, Wrapper> dict)
    {
        foreach (var (_,wrapper) in dict)
        {
            Console.WriteLine(wrapper.X);
        }
    }
    public static void Main2(Dictionary<Guid, Wrapper> dict)
    {
        foreach (var (_,wrapper) in dict)
        {
            Console.WriteLine(wrapper.X);
        }
    }
}