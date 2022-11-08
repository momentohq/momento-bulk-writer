using CommandLine;
using Momento.Etl.Utils;

namespace Momento.Etl.RedisLoadGenerator;

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

    public void Validate()
    {
        OptionUtils.AssertNonnegative(StartupDelay, "StartupDelay");
        OptionUtils.AssertStrictlyPositive(MaxItemsPerDataStructure, "MaxItemsPerDataStructure");
        OptionUtils.AssertStrictlyPositive(MaxTtlHours, "MaxTtlHours");
        OptionUtils.AssertInUnitInterval(ExpireProb, "ExpireProb");
        OptionUtils.AssertNonnegative(RandomSeed, "RandomSeed");
        OptionUtils.AssertStrictlyPositive(NumItems, "NumItems");
    }
}
