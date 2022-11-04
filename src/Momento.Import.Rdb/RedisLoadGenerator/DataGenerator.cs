using System.Text;

namespace Momento.Import.Rdb.RedisLoadGenerator;

internal class DataGenerator
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
    /// <param name="maxHours"></param>
    /// <returns></returns>
    public TimeSpan RandomTimeSpan(int? maxHours = null)
    {
        var maxHoursInSeconds = (int)Math.Round(TimeSpan.FromHours(maxHours ?? maxTtlHours).TotalSeconds);
        var randomSeconds = rnd.Next(1, maxHoursInSeconds);
        return TimeSpan.FromSeconds(randomSeconds);
    }

    public string RandomishString(int num16ByteBlocks = 1)
    {
        StringBuilder sb = new();
        for (var i = 0; i <= num16ByteBlocks; i++)
        {
            sb.Append(Guid.NewGuid().ToString());
        }
        return sb.ToString();
    }

    public string Randomish1KBString()
    {
        return RandomishString(32);
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
