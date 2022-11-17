using System.Linq;
using System.Text;
using CommandLine;
using Microsoft.Extensions.Logging;
using Momento.Etl.Utils;
using Momento.Etl.Validation;

namespace Momento.Etl.Cli.Split;

public class Command
{
    private ILogger logger;

    public Command(ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger<Command>();
    }

    public async Task RunAsync(Options options)
    {
        logger.LogInformation($"Splitting {options.DataFilePath} into {options.NumChunks} chunks with file prefix {options.OutputPrefix}");

        var numLines = CountLines(options.DataFilePath);
        if (options.NumChunks > numLines)
        {
            logger.LogError($"More chunks than lines: {options.NumChunks} chunks vs {numLines} lines. Exiting");
            Environment.Exit(1);
        }

        var numLinesPerChunk = numLines / options.NumChunks;
        // Adjust up if the split is uneven to ensure all lines get written
        numLinesPerChunk += numLines % options.NumChunks;

        logger.LogInformation($"{numLines} total and splitting into {numLinesPerChunk} lines per chunk");

        using var inputStream = File.OpenText(options.DataFilePath);
        var linesProcessed = 0;
        foreach (var chunk in Enumerable.Range(1, options.NumChunks))
        {
            var chunkString = IntToStringWithPadding(chunk, options.NumChunks);
            var outputFilePath = options.OutputPrefix + chunkString;
            linesProcessed += await WriteChunk(inputStream, numLinesPerChunk, outputFilePath);
        }
        logger.LogInformation($"Processed {linesProcessed} lines");
    }

    private int CountLines(string filepath)
    {
        using var inputStream = File.OpenText(filepath);
        int numLines = 0;
        while (inputStream.ReadLine() != null)
        {
            numLines++;
        }
        return numLines;
    }

    private async Task<int> WriteChunk(StreamReader inputStream, int numLinesPerChunk, string outputFilePath)
    {
        using var outStream = new StreamWriter(outputFilePath, append: false);
        var lineNumberInChunk = 0;
        string? line;
        while (lineNumberInChunk < numLinesPerChunk && (line = inputStream.ReadLine()) != null)
        {
            await outStream.WriteLineAsync(line);
            lineNumberInChunk++;
        }
        return lineNumberInChunk;
    }

    /// <summary>
    /// Convert int to string and pad with leading zeros until as long as reference number.
    /// </summary>
    /// <param name="number">Number to convert to string with padding</param>
    /// <param name="paddingReferenceNumber">Number to use to calculate zero padding</param>
    /// <returns></returns>
    private string IntToStringWithPadding(int number, int paddingReferenceNumber)
    {
        var intString = $"{number}";
        var refenceNumber = $"{paddingReferenceNumber}";
        var numLeadingZeros = Math.Max(0, refenceNumber.Length - intString.Length);
        var sb = new StringBuilder();
        foreach (var i in Enumerable.Range(1, numLeadingZeros))
        {
            sb.Append("0");
        }
        sb.Append(intString);
        return sb.ToString();
    }
}

