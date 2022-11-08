using CommandLine;
using Microsoft.Extensions.Logging;
using Momento.Etl.Validation;

namespace Momento.Etl.Cli.Validate;

public class Command
{
    private ILogger logger;
    private Stats stats = null!;

    public Command(ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger<Command>();
    }

    public async Task RunAsync(Options options)
    {
        stats = new Stats(logger);
        try
        {
            options.Validate();
        }
        catch (Exception e)
        {
            logger.LogError($"Error validating CLI options: {e.Message}");
            await Task.Delay(1);
            Environment.Exit(1);
        }

        var dataValidators = new DataValidatorChain();
        if (options.FilterLargeData)
        {
            logger.LogInformation($"Filtering payloads larger than {options.MaxPayloadSize}MiB");
            dataValidators.AddDataValidator(new PayloadSizeValidator(options.MaxPayloadSize));
        }
        if (options.FilterLongTtl)
        {
            logger.LogInformation($"Filtering items with TTL greater than {options.MaxTtl} days");
            dataValidators.AddDataValidator(new TtlInRangeValidator(TimeSpan.FromDays(options.MaxTtl)));
        }
        if (options.FilterAlreadyExpired)
        {
            logger.LogInformation($"Filtering items that have already expired");
            dataValidators.AddDataValidator(new HasntAlreadyExpiredValidator());
        }
        if (options.FilterMissingTtl)
        {
            logger.LogInformation($"Filtering items with no TTL set");
            dataValidators.AddDataValidator(new HasTtlValidator());
        }
        logger.LogInformation("");

        logger.LogInformation($"Reading data from {options.DataFilePath}");
        logger.LogInformation($"Writing valid to {options.ValidFilePath}");
        logger.LogInformation($"Writing errors to {options.ErrorFilePath}");

        using var inputStream = File.OpenText(options.DataFilePath);
        using var validStream = new StreamWriter(options.ValidFilePath, append: false);
        using var errorStream = new StreamWriter(options.ErrorFilePath, append: false);

        string? line;
        while ((line = inputStream.ReadLine()) != null)
        {
            await ProcessLine(line, dataValidators, validStream, errorStream);
        }

        logger.LogInformation("Finished");
        stats.LogStats();
    }

    private async Task ProcessLine(string line, IDataValidator dataValidator, StreamWriter validStream, StreamWriter errorStream)
    {
        line = line.Trim();
        if (line.Equals(""))
        {
            return;
        }

        var jsonParseResult = RdbJsonReader.ParseJson(line);
        if (jsonParseResult is JsonParseResult.OK ok)
        {
            var validationResult = dataValidator.Validate(ok.Item);
            if (validationResult is ValidationResult.OK)
            {
                await validStream.WriteLineAsync(line);
                stats.OK++;
            }
            else if (validationResult is ValidationResult.Error error)
            {
                await errorStream.WriteLineAsync($"{error.Message}\t{line}");
                stats.Error++;
                stats.IncrementSpecificErrorCount(error.Message);
            }
            else
            {
                logger.LogError($"Error validating line, got unknown result {validationResult} ; line = {line}");
            }
        }
        else if (jsonParseResult is JsonParseResult.Error error)
        {
            await errorStream.WriteLineAsync($"{error.Message}\t{line}");
            stats.Error++;
            stats.IncrementSpecificErrorCount(error.Message);
        }
        else
        {
            logger.LogError($"Error parsing line, got unknown result {jsonParseResult} ; line={line}");
        }
        stats.Total++;
    }

    private record Stats
    {
        public int Total { get; set; }
        public int OK { get; set; }
        public int Error { get; set; }
        public Dictionary<string, int> SpecificErrorCounts { get; set; } = new();

        private ILogger logger;
        public Stats(ILogger logger)
        {
            this.logger = logger;
        }
        public void LogStats(LogLevel level = LogLevel.Information)
        {
            logger.Log(level, "");
            logger.Log(level, "==== STATS ====");
            logger.Log(level, $"Total: {Total}");
            logger.Log(level, $"OK: {OK}");
            logger.Log(level, $"Error: {Error}");
            logger.Log(level, "----");
            foreach (var item in SpecificErrorCounts)
            {
                logger.Log(level, $"{item.Key}: {item.Value}");
            }
        }

        public void IncrementSpecificErrorCount(string name)
        {
            if (SpecificErrorCounts.ContainsKey(name))
            {
                SpecificErrorCounts[name]++;
            }
            else
            {
                SpecificErrorCounts[name] = 1;
            }
        }
    }
}

