using System;

namespace Semtex.UT.NotSemanticallyEquivalent.StuffInRegion;

public class Left
{
    #region MyRegion

    public void RealMethod()
    {
        Console.WriteLine("Does something");
    }


    #endregion
}