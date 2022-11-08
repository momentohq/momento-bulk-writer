using CommandLine;

namespace Momento.Etl.Cli.Load;

[Verb("load", HelpText = "load data into momento")]
public class Options
{
    [Value(0, MetaName = "DATA_PATH", Required = true, HelpText = "Path to read redis-rdb-cli dump from.")]
    public string DataFilePath { get; set; } = default!;

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
    }
}
