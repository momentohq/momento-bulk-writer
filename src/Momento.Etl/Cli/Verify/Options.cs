using CommandLine;
using Momento.Etl.Utils;

namespace Momento.Etl.Cli.Verify;

[Verb("verify", HelpText = "verify loaded data in momento")]
public class Options
{
    [Option('a', "authToken", Required = true, HelpText = "Momento auth token.")]
    public string AuthToken { get; set; } = default!;

    [Option('c', "cacheName", Required = true, HelpText = "Momento cache to store data in.")]
    public string CacheName { get; set; } = default!;

    [Option('n', "numberOfConcurrentRequests", Required = false, HelpText = "Number of concurrent requests. Defaults to 1.")]
    public int NumberOfConcurrentRequests { get; set; } = 1;

    [Value(0, Required = false, HelpText = "File to load into Momento")]
    public string FilePath { get; set; } = default!;

    public void Validate()
    {
        OptionUtils.TryOpenFile(FilePath);
    }
}
