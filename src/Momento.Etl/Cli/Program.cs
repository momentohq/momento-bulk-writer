// See https://aka.ms/new-console-template for more information
using System;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;

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
                var command = new Validate.Command(loggerFactory);
                await command.RunAsync(options);
            });
        result = await result.WithParsedAsync<Load.Options>(async options =>
            {
                var command = new Load.Command(loggerFactory, options.AuthToken);
                await command.RunAsync(options);
            });
        result.WithNotParsed(errors => DisplayHelp(result, errors));
    }
}

