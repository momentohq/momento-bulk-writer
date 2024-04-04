using System;
using System.Text;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;
using Momento.Etl.Cli;
using Momento.Etl.Model;
using Momento.Etl.Utils;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;
namespace Momento.Etl.RedisLoadGenerator;

public class Program
{
    private static ILogger logger;
    private static TimeSpan defaultTtl;
    private static IDatabase client = null!;
    private static long zaddOperationsCounter = 0;
    private static System.Timers.Timer throughputTimer;

    static Program()
    {
        var loggerFactory = LoggerUtils.CreateConsoleLoggerFactory();
        logger = loggerFactory.CreateLogger<Program>();


        // Initialize and start the throughput timer
        throughputTimer = new System.Timers.Timer(1000); // Log every 1000 milliseconds (1 second)
        throughputTimer.Elapsed += LogThroughput;
        throughputTimer.AutoReset = true;
        throughputTimer.Enabled = true;
    }

    public static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errors)
    {
        var helpText = HelpText.AutoBuild(result, h =>
        {
            h.AdditionalNewLineAfterOption = false;
            h.Heading = "Redis Reload 0.0.1-beta";
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

        try
        {
            client = await ConnectToRedis(options.RedisHost, options.RedisPort);
        }
        catch (RedisConnectionException e)
        {
            logger.LogError($"Could not connect to redis: {e.Message}");
            Environment.Exit(1);
        }

        logger.LogInformation($"Using default TTL of {options.DefaultTtl}d");
        defaultTtl = TimeSpan.FromDays(options.DefaultTtl);

        logger.LogInformation($"Loading from {options.RedisDumpJsonlPath} items");

        using (var stream = File.OpenText(options.RedisDumpJsonlPath))
        {
            string? line;
            int linesProcessed = 0;
            while ((line = stream.ReadLine()) != null)
            {
                await ProcessLine(line);
                linesProcessed++;
                if (linesProcessed % 10_000 == 0)
                {
                    logger.LogInformation($"Processed {linesProcessed}");
                }
            }
        }

        logger.LogInformation("All done");
    }

    private static async Task ProcessLine(string line)
    {
        line = line.Trim();
        if (line.Equals(""))
        {
            return;
        }

        var result = RdbJsonReader.ParseJson(line);
        if (result is JsonParseResult.OK ok)
        {
            var item = ok.Item;
            await Load(item as dynamic);
            if (item.Expiry.HasValue)
            {
                await client.KeyExpireAsync(item.Key, defaultTtl);
            }
        }
        else if (result is JsonParseResult.Error error)
        {
            logger.LogError($"{error.Message}: {line}");
        }
        else
        {
            logger.LogError($"should not reach here: {line}");
        }
    }

    private static async Task Load(RedisString item)
    {
        await client.StringSetAsync(item.Key, item.Value);
    }

    private static async Task Load(RedisHash item)
    {
        foreach (var kv in item.Value)
        {
            await client.HashSetAsync(item.Key, kv.Key, kv.Value);
        }
    }

    private static async Task Load(RedisSortedSet item)
    {
        foreach (var kv in item.Value)
        {
            await LogAndLoadAsync(() => client.SortedSetAddAsync(item.Key, kv.Key, kv.Value), "sortedset", "ZADD");
        }
    }

    private static async Task Load(RedisList item)
    {
        foreach (var value in item.Value)
        {
            await client.ListRightPushAsync(item.Key, value);
        }
    }

    private static async Task Load(RedisSet item)
    {
        foreach (var value in item.Value)
        {
            await client.SetAddAsync(item.Key, value);
        }
    }

    private static async Task LogAndLoadAsync(Func<Task> loadOperation, string itemType, string redisAPI)
    {
        var stopwatch = Stopwatch.StartNew();

        await loadOperation();

        stopwatch.Stop();
        long durationMicroseconds = (stopwatch.ElapsedTicks * 1_000_000) / Stopwatch.Frequency;

        // If this is a ZADD operation, increment the counter
        if (redisAPI == "ZADD")
        {
            Interlocked.Increment(ref zaddOperationsCounter);
        }

        var logEntry = new
        {
            duration = durationMicroseconds,
            itemType,
            redisAPI
        };

        logger.LogInformation(JsonSerializer.Serialize(logEntry));
    }


    private static void LogThroughput(Object source, System.Timers.ElapsedEventArgs e)
    {
        // Capture the counter value and reset it atomically
        long operationsThisSecond = Interlocked.Exchange(ref zaddOperationsCounter, 0);
        
        // Log the throughput
        logger.LogInformation($"ZADD Throughput: {operationsThisSecond} operations per second");
    }



#pragma warning disable CS1998
    // disable warnings for no "await"
    private static async Task Load(object item)
    {
        logger.LogError("LoadError: unsupported_data_type");
    }
#pragma warning restore CS1998

}
