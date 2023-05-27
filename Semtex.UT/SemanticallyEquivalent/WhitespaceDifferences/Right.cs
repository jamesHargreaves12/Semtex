using System;

namespace Semtex.UT.ShouldPass.WhitespaceDifferences;



public class Right
{
    
    private readonly string _name;

    
    public Right(
        string name )
    {
        
        _name = name;
    }

    public string GiveMeTheNameWithSuffix(int suffix)
    {
        
        var newName  =  _name  +  suffix.ToString( );

        Console.WriteLine( $"Output = {newName}" );
        
        return newName ;
    }
    
    
   
}

