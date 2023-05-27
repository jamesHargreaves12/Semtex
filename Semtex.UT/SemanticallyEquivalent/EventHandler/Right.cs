using System;

namespace Semtex.UT.ShouldPass.EventHandlerNs;

public class Right
{
    public void M()
    {
        ChangedA += ChangedB;
    }

    public void ChangedB(object sender, EventArgs e) { }

    public event EventHandler ChangedA;
}