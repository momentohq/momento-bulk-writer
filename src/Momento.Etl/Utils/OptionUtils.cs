namespace Momento.Etl.Utils;

public static class OptionUtils
{
    public static void TryOpenFile(string filePath)
    {
        var stream = File.OpenRead(filePath);
        stream.Dispose();
    }

    public static void AssertStrictlyPositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentException("Number was 0 or negative and must be strictly positive", name);
        }
    }

    public static void AssertNonnegative(int value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentException("Number was negative but must be non-negative", name);
        }
    }

    public static void AssertInUnitInterval(double value, string name)
    {
        if (value < 0 || value > 1)
        {
            throw new ArgumentException("Number was negative or great than 1 and must be in the unit interval", name);
        }
    }
}
