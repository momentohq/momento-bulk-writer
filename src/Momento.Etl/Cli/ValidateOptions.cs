using CommandLine;

namespace Momento.Etl.Cli;

public class ValidateOptions
{
    [Option("maxPayloadSize", Required = false, HelpText = "Max payload size in MiB, inclusive. Defaults to 1.")]
    public int MaxPayloadSize { get; set; } = 1;

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

    private static void TryOpenFile(string filePath)
    {
        var stream = File.OpenRead(filePath);
        stream.Dispose();
    }

    private static void AssertStrictlyPositive(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentException("Number was 0 or negative and must be strictly positive", name);
        }
    }

    public void Validate()
    {
        TryOpenFile(DataFilePath);
        AssertStrictlyPositive(MaxPayloadSize, "maxPayloadSize");
        AssertStrictlyPositive(MaxTtl, "maxTtl");
    }
}
