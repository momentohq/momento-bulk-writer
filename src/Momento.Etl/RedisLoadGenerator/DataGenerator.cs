using System.Text;

namespace Momento.Etl.RedisLoadGenerator;

/// <summary>
/// Generates somewhat-random data to store in Redis.
///
/// For convenience we generate Guids and concatenate them together.
/// Because of this, the data is not completely random.
/// That is OK though: only the keys need not collide.
/// Other randomly generated data to store as values in Redis are just to fill up space.
/// </summary>
public class DataGenerator
{
    private Random rnd;
    private int maxItemsPerDataStructure;
    private int maxTtlHours;
    private double expireProbability;

    public DataGenerator(int maxItemsPerDataStructure, int maxTtlHours, double expireProbability, int randomSeed = 42)
    {
        rnd = new Random(randomSeed);
        this.maxItemsPerDataStructure = maxItemsPerDataStructure;
        this.maxTtlHours = maxTtlHours;
        this.expireProbability = expireProbability;
    }

    public bool ShouldExpire(double? expireProbability = null)
    {
        return rnd.NextDouble() <= (expireProbability ?? this.expireProbability);
    }

    /// <summary>
    /// Generates a random TimeSpan between 1 second and <paramref name="maxHours" /> seconds.
    /// </summary>
    /// <param name="maxHours">Max time in hours for the TimeSpan to generate. Defaults to value in constructor.</param>
    /// <returns></returns>
    public TimeSpan RandomTimeSpan(int? maxHours = null)
    {
        var maxHoursInSeconds = (int)Math.Round(TimeSpan.FromHours(maxHours ?? maxTtlHours).TotalSeconds);
        var randomSeconds = rnd.Next(1, maxHoursInSeconds);
        return TimeSpan.FromSeconds(randomSeconds);
    }

    /// <summary>
    /// Generate a random-enough string concatenating Guids together.
    /// </summary>
    /// <param name="numBytes"></param>
    /// <returns></returns>
    public string RandomishString(int numBytes = 16)
    {
        int num16ByteBlocks = numBytes / 16;
        if (numBytes % 16 > 0)
        {
            num16ByteBlocks++;
        }
        StringBuilder sb = new();
        for (var i = 0; i < num16ByteBlocks; i++)
        {
            sb.Append(Guid.NewGuid().ToString());
        }
        var result = sb.ToString();
        if (result.Length > numBytes)
        {
            result = result.Substring(0, numBytes);
        }
        return result;
    }

    public string Randomish1KBString()
    {
        return RandomishString(1024);
    }

    public int NumItemsPerDataStructure(int? maxItemsPerDataStructure = null)
    {
        return rnd.Next(1, maxItemsPerDataStructure ?? this.maxItemsPerDataStructure);
    }

    public DataType RandomDataType()
    {
        // We can parameterize this distribution if it becomes important
        var probability = rnd.NextDouble();
        if (probability <= .5)
        {
            return DataType.STRING;
        }
        else if (probability <= .66)
        {
            return DataType.SET;
        }
        else if (probability <= .83)
        {
            return DataType.HASH;
        }
        else
        {
            return DataType.LIST;
        }
    }
}
