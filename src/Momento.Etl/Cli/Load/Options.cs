using CommandLine;
using Momento.Etl.Utils;

namespace Momento.Etl.Cli.Load;

[Verb("load", HelpText = "load data into momento")]
public class Options
{
    [Option('a', "authToken", Required = true, HelpText = "Momento auth token.")]
    public string AuthToken { get; set; } = default!;

    [Option('c', "cacheName", Required = true, HelpText = "Momento cache to store data in.")]
    public string CacheName { get; set; } = default!;

    [Option('x', "createCache", Required = false, HelpText = "Create cache if not present.")]
    public bool CreateCache { get; set; }

    [Option("defaultTtl", Required = true, HelpText = "Default TTL in days, inclusive. Items missing a TTL receive this value. Should be less than or equal to max TTL.")]
    public int DefaultTtl { get; set; }

    [Option('r', "resetAlreadyExpiredToDefaultTtl", Required = false, HelpText = "(For testing) Reset an already expired item to default TTL. Defaults to false.")]
    public bool ResetAlreadyExpiredToDefaultTtl { get; set; } = false;

    [Value(0, Required = false, HelpText = "File to load into Momento")]
    public string FilePath { get; set; } = default!;

    public TimeSpan DefaultTtlTimeSpan { get => TimeSpan.FromDays(DefaultTtl); private set { } }

    public void Validate()
    {
        OptionUtils.TryOpenFile(FilePath);
        OptionUtils.AssertStrictlyPositive(DefaultTtl, "defaultTtl");
    }
}
