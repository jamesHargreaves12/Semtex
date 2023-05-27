using System;

namespace Semtex.UT.ShouldPass.EventHandlerNs;

public class Left
{
    public void M()
    {
        ChangedA += new EventHandler(ChangedB);
    }
    public void ChangedB(object sender, EventArgs e) { }
    public event EventHandler ChangedA;
}
