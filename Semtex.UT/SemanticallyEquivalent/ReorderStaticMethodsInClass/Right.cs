namespace Semtex.UT.ShouldPass.ReorderMethodsInClass;

public class Right
{
    public static long Sixth(int value)
    {
        return value * 2;
    }
    
    public static long Fifth(int value)
    {
        return (long)value;
    }
    
    public static int Third()
    {
        return 0;
    }
    
    public static int Fourth()
    {
        return 1;
    }
    
    public static void Second() { }
    
    public static void First() { }
}