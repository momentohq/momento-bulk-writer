// See https://aka.ms/new-console-template for more information
using System;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;
using Momento.Sdk.Auth;
using Momento.Sdk.Config;
using Momento.Sdk.Incubating;


namespace Momento.Etl.Cli;

public class Program
{
    private static ILoggerFactory loggerFactory;
    private static ILogger logger;

    static Program()
    {
        loggerFactory = LoggerFactory.Create(builder =>
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
            h.Heading = "Momento ETL 0.0.1-beta";
            h.Copyright = "Copyright (c) 2022 Momento";
            return HelpText.DefaultParsingErrorsHandler(result, h);
        }, e => e);
        Console.WriteLine(helpText);
    }

    public static async Task Main(string[] args)
    {
        var parser = new CommandLine.Parser(with => with.HelpWriter = null);
        var result = parser.ParseArguments<Validate.Options, Load.Options>(args);
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

            var config = Configurations.InRegion.Default.Latest(loggerFactory);
            var authProvider = new StringMomentoTokenProvider(options.AuthToken);
            var client = SimpleCacheClientFactory.CreateClient(config, authProvider, TimeSpan.FromMinutes(1));

            var command = new Load.Command(loggerFactory, client);
            await command.RunAsync(options);
        });
        result.WithNotParsed(errors => DisplayHelp(result, errors));
    }
}

