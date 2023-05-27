namespace Semtex.UT.ShouldPass.ReorderMethodsInClass;

public class Left
{
    public static void First() { }
    public static void Second() { }

    public static int Third()
    {
        return 0;
    }

    public static int Fourth()
    {
        return 1;
    }

    public static long Fifth(int value)
    {
        return (long)value;
    }

    public static long Sixth(int value)
    {
        return value * 2;
    }
}