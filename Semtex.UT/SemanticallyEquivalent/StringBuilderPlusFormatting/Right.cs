using System;
using System.Text;

namespace Semtex.UT.SemanticallyEquivalent.StringBuilderPlusFormatting;

public class Right
{
    public string Format(DateTime x)
    {
        var sb = new StringBuilder();

        return sb.Append($@"{x:hh\:mm\:ss\.fff}").ToString();
    }

}