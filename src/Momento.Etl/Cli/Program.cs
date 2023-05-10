// See https://aka.ms/new-console-template for more information
using System;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;
using Momento.Etl.Utils;
using Momento.Sdk;
using Momento.Sdk.Auth;
using Momento.Sdk.Config;

namespace Momento.Etl.Cli;

public class Program
{
    private static ILoggerFactory loggerFactory;
    private static ILogger logger;

    static Program()
    {
        loggerFactory = LoggerUtils.CreateConsoleLoggerFactory();
        logger = loggerFactory.CreateLogger<Program>();
    }

    public static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errors)
    {
        var helpText = HelpText.AutoBuild(result, h =>
        {
            h.AdditionalNewLineAfterOption = false;
            h.Heading = "Momento ETL 0.0.1-beta";
            h.Copyright = "Copyright (c) 2022 Momento";
            return HelpText.DefaultParsingErrorsHandler(result, h);
        }, e => e);
        Console.WriteLine(helpText);
    }

    public static async Task Main(string[] args)
    {
        var parser = new CommandLine.Parser(with => with.HelpWriter = null);
        var result = parser.ParseArguments<Validate.Options, Split.Options, Load.Options, Verify.Options>(args);
        result = await result.WithParsedAsync<Validate.Options>(async options =>
        {
            try
            {
                options.Validate();
            }
            catch (Exception e)
            {
                logger.LogError($"Error validating CLI options: {e.Message}");
                await ExitUtils.DelayedExit(1);
            }

            var command = new Validate.Command(loggerFactory);
            await command.RunAsync(options);
        });
        result = await result.WithParsedAsync<Split.Options>(async options =>
        {
            try
            {
                options.Validate();
            }
            catch (Exception e)
            {
                logger.LogError($"Error validating CLI options: {e.Message}");
                await ExitUtils.DelayedExit(1);
            }

            var command = new Split.Command(loggerFactory);
            await command.RunAsync(options);
        });
        result = await result.WithParsedAsync<Load.Options>(async options =>
        {
            try
            {
                options.Validate();
            }
            catch (Exception e)
            {
                logger.LogError($"Error validating CLI options: {e.Message}");
                await ExitUtils.DelayedExit(1);
            }

            logger.LogInformation($"Loading to {options.CacheName} with a default TTL of {options.DefaultTtlTimeSpan} and clipping excessive TTLs to the cache limit.");
            // Previously we used the InRegion.Latest config. Because we can saturate the network when doing an import,
            // we opt to use a config we more relaxed timeouts.
            var config = Configurations.Laptop.Latest(loggerFactory);
            var authProvider = new StringMomentoTokenProvider(options.AuthToken);
            var client = new CacheClient(config, authProvider, options.DefaultTtlTimeSpan);

            var command = new Load.Command(loggerFactory, client, options.CreateCache);
            await command.RunAsync(options.CacheName, options.FilePath, options.ResetAlreadyExpiredToDefaultTtl, options.MaxNumberOfConcurrentRequests);
        });
        result = await result.WithParsedAsync<Verify.Options>(async options =>
        {
            try
            {
                options.Validate();
            }
            catch (Exception e)
            {
                logger.LogError($"Error validating CLI options: {e.Message}");
                await ExitUtils.DelayedExit(1);
            }

            logger.LogInformation($"Verifying {options.CacheName}.");
            // Previously we used the InRegion.Latest config. Because we can saturate the network when doing an import,
            // we opt to use a config we more relaxed timeouts.
            var config = Configurations.Laptop.Latest(loggerFactory);
            var authProvider = new StringMomentoTokenProvider(options.AuthToken);
            var client = new CacheClient(config, authProvider, TimeSpan.FromMinutes(1));

            var command = new Verify.Command(loggerFactory, client);
            await command.RunAsync(options.CacheName, options.FilePath, options.MaxNumberOfConcurrentRequests);
        });
        result.WithNotParsed(errors => DisplayHelp(result, errors));
    }
}

