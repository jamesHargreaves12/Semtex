using System;
using System.Collections.Generic;

namespace Semtex.UT.SemanticallyEquivalent.VariableOverlap;

public class Left
{
    public static void M(Dictionary<string, string> settings, string name)
    {
        if (name == "name1")
        {
            if (settings.TryGetValue("name1", out var v1))
            {
                Console.WriteLine(v1);
            }
        }
        
        if (name == "name2")
        {
            if (settings.TryGetValue("name2", out var v1))
            {
                Console.WriteLine(v1);
            }
        }
    }
}