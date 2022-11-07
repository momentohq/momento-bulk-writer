using CommandLine;

public class Options
{
    [Option("redisHost", Required = false, HelpText = "Redis host to connect to")]
    public string RedisHost { get; set; } = "redis";

    [Option("redisPort", Required = false, HelpText = "Redis port to connect to")]
    public int RedisPort { get; set; } = 6379;

    [Option("startupDelay", Required = false, HelpText = "Number of seconds to wait for Redis to start before running load gen.")]
    public int StartupDelay { get; set; } = 3;

    [Option("maxItemsPerDataStructure", Required = false, HelpText = "Maximum number of items to add to a data structure. Adds a random quantity between 1 and max inclusive.")]
    public int MaxItemsPerDataStructure { get; set; } = 1_000;

    [Option("maxTtlHours", Required = false, HelpText = "Maximum TTL in hours to apply to items that get an expiry.")]
    public int MaxTtlHours { get; set; } = 48;

    [Option("expireProb", Required = false, HelpText = "Assign at random this fraction of items an expiry. Must be in [0, 1]. Defaults to 0.")]
    public double ExpireProb { get; set; } = 0;

    [Option("randomSeed", Required = false, HelpText = "Random seed.")]
    public int RandomSeed { get; set; } = 42;

    [Value(0, Required = true, HelpText = "Number of items to generate.")]
    public int NumItems { get; set; }

    private static void AssertNonnegative(int value, string name)
    {
        if (value < 0)
        {
            throw new ArgumentException("Number was negative but must be non-negative", name);
        }
    }

    private static void AssertStrictlyPositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentException("Number was 0 or negative and must be strictly positive", name);
        }
    }

    private static void AssertInUnitInterval(double value, string name)
    {
        if (value < 0 || value > 1)
        {
            throw new ArgumentException("Number was negative or great than 1 and must be in the unit interval", name);
        }
    }

    public void Validate()
    {
        AssertNonnegative(StartupDelay, "StartupDelay");
        AssertStrictlyPositive(MaxItemsPerDataStructure, "MaxItemsPerDataStructure");
        AssertStrictlyPositive(MaxTtlHours, "MaxTtlHours");
        AssertInUnitInterval(ExpireProb, "ExpireProb");
        AssertNonnegative(RandomSeed, "RandomSeed");
        AssertStrictlyPositive(NumItems, "NumItems");
    }
}
