using System;
using System.Text;

namespace Semtex.UT.SemanticallyEquivalent.StringBuilderPlusFormatting;

public class Left
{
    public string Format(DateTime x)
    {
        var sb = new StringBuilder();

        return sb.AppendFormat(@"{0:hh\:mm\:ss\.fff}", x).ToString();
    }
}