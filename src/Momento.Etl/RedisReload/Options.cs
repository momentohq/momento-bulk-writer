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

    [Option("DefaulTtl", Required = false, HelpText = "TTL in days to use to overwrite existing TTLs.")]
    public int DefaultTtl { get; set; } = 5000;

    [Value(0, Required = true, HelpText = "Path to Redis dump as JSONL.")]
    public string RedisDumpJsonlPath { get; set; } = default!;

    public void Validate()
    {
        OptionUtils.TryOpenFile(RedisDumpJsonlPath);
        OptionUtils.AssertStrictlyPositive(DefaultTtl, "DefaultTtl");
    }
}
