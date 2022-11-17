using CommandLine;
using Momento.Etl.Utils;

namespace Momento.Etl.Cli.Split;

[Verb("split", HelpText = "split file into equal sized chunks")]
public class Options
{
    [Option('n', "numChunks", Required = true, HelpText = "Number of chunks to write. Defaults to 1.")]
    public int NumChunks { get; set; } = 1;

    [Value(0, MetaName = "DATA_PATH", Required = true, HelpText = "Path to input data.")]
    public string DataFilePath { get; set; } = default!;

    [Value(1, MetaName = "OUTPUT_PREFIX", Required = true, HelpText = "Path to write chunks to. Writes filenames with this prefix and numeric suffix.")]
    public string OutputPrefix { get; set; } = default!;

    public void Validate()
    {
        OptionUtils.TryOpenFile(DataFilePath);
        OptionUtils.AssertStrictlyPositive(NumChunks, "numChunks");
    }
}
