using CommandLine;
using Momento.Etl.Utils;

namespace Momento.Etl.Cli.Validate;

[Verb("validate", HelpText = "validate redis data for momento")]
public class Options
{
    [Option("maxItemSize", Required = false, HelpText = "Max item size in MiB, inclusive. Defaults to 1.")]
    public int MaxItemSize { get; set; } = 1;

    [Option("filterLargeData", Required = false, HelpText = "Test for payloads that exceed the max. Defaults to true.")]
    public bool FilterLargeData { get; set; } = true;

    [Option("maxTtl", Required = false, HelpText = "Max TTL in days, inclusive. Defaults to 1.")]
    public int MaxTtl { get; set; } = 1;

    [Option("filterLongTtl", Required = false, HelpText = "Test for TTLs that exceed the max. Defaults to false.")]
    public bool FilterLongTtl { get; set; } = false;

    [Option("filterAlreadyExpired", Required = false, HelpText = "Test for items that have already expired. Defaults to false.")]
    public bool FilterAlreadyExpired { get; set; } = false;

    [Option("filterMissingTtl", Required = false, HelpText = "Test for items with no TTLs. Defaults to false.")]
    public bool FilterMissingTtl { get; set; } = false;

    [Value(0, MetaName = "DATA_PATH", Required = true, HelpText = "Path to read redis-rdb-cli dump from.")]
    public string DataFilePath { get; set; } = default!;

    [Value(1, MetaName = "VALID_PATH", Required = true, HelpText = "Path to write valid data to.")]
    public string ValidFilePath { get; set; } = default!;

    [Value(2, MetaName = "ERROR_PATH", Required = true, HelpText = "Path to write invalid data to.")]
    public string ErrorFilePath { get; set; } = default!;

    public void Validate()
    {
        OptionUtils.TryOpenFile(DataFilePath);
        OptionUtils.AssertStrictlyPositive(MaxItemSize, "maxItemSize");
        OptionUtils.AssertStrictlyPositive(MaxTtl, "maxTtl");
    }
}
