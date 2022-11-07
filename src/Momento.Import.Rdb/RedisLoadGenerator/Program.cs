using System;
using System.Text;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Momento.Import.Rdb.RedisLoadGenerator;

public class Program
{
    private static ILogger logger;

    static Program()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
            });
            builder.SetMinimumLevel(LogLevel.Information);
        });
        logger = loggerFactory.CreateLogger<Program>();
    }

    public static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errors)
    {
        var helpText = HelpText.AutoBuild(result, h =>
        {
            h.AdditionalNewLineAfterOption = false;
            h.Heading = "Redis Load Generator 0.0.1-beta";
            h.Copyright = "Copyright (c) 2022 Momento";
            return HelpText.DefaultParsingErrorsHandler(result, h);
        }, e => e);
        Console.WriteLine(helpText);
    }

    public static async Task Main(string[] args)
    {
        var parser = new CommandLine.Parser(with => with.HelpWriter = null);
        var parserResult = parser.ParseArguments<Options>(args);
        parserResult.WithNotParsed<Options>(errors => DisplayHelp(parserResult, errors));
        await parserResult.WithParsedAsync<Options>(async options => await RunAsync(options));
    }

    private static async Task<IDatabase> ConnectToRedis(string host, int port)
    {
        var redisOptions = new ConfigurationOptions()
        {
            EndPoints = { { host, port } }
        };
        var connection = (await ConnectionMultiplexer.ConnectAsync(redisOptions));
        return connection.GetDatabase();
    }

    public static async Task RunAsync(Options options)
    {
        try
        {
            options.Validate();
        }
        catch (Exception e)
        {
            logger.LogError($"Error validating CLI options: {e.Message}");
            Environment.Exit(1);
        }

        logger.LogInformation($"Waiting {options.StartupDelay} seconds for Redis to load...");
        await Task.Delay(options.StartupDelay * 1000);

        logger.LogInformation($"Connecting to Redis at {options.RedisHost}:{options.RedisPort}");
        IDatabase client = null!;
        try
        {
            client = await ConnectToRedis(options.RedisHost, options.RedisPort);
        }
        catch (RedisConnectionException e)
        {
            logger.LogError($"Could not connect to redis: {e.Message}");
            Environment.Exit(1);
        }

        logger.LogInformation($"Generating {options.NumItems} items");
        var dataGenerator = new DataGenerator(
            maxItemsPerDataStructure: options.MaxItemsPerDataStructure,
            expireProbability: options.ExpireProb,
            maxTtlHours: options.MaxTtlHours,
            randomSeed: options.RandomSeed);

        foreach (int num in Enumerable.Range(1, options.NumItems))
        {
            var key = dataGenerator.RandomishString();
            switch (dataGenerator.RandomDataType())
            {
                case DataType.STRING:
                    await client.StringSetAsync(key, dataGenerator.Randomish1KBString());
                    break;
                case DataType.SET:
                    for (int i = 0; i < dataGenerator.NumItemsPerDataStructure(); i++)
                    {
                        await client.SetAddAsync(key, dataGenerator.Randomish1KBString());
                    }
                    break;
                case DataType.HASH:
                    for (int i = 0; i < dataGenerator.NumItemsPerDataStructure(); i++)
                    {
                        await client.HashSetAsync(key, dataGenerator.Randomish1KBString(), dataGenerator.Randomish1KBString());
                    }
                    break;
                case DataType.LIST:
                    for (int i = 0; i < dataGenerator.NumItemsPerDataStructure(); i++)
                    {
                        await client.ListRightPushAsync(key, dataGenerator.Randomish1KBString());
                    }
                    break;
            }

            if (dataGenerator.ShouldExpire())
            {
                await client.KeyExpireAsync(key, dataGenerator.RandomTimeSpan());
            }

            if ((num + 1) % 50_000 == 0)
            {
                logger.LogInformation($"Finished {num + 1}");
            }
        }
        logger.LogInformation("All done");
    }
}

