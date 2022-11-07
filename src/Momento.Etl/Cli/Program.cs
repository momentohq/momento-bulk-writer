﻿// See https://aka.ms/new-console-template for more information
using System;
using System.Text;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Logging;
using Momento.Etl.Validation;

namespace Momento.Etl.Cli;

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
            h.Heading = "Momento ETL 0.0.1-beta";
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

    public static async Task RunAsync(Options options)
    {
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
    }

    private static async Task ProcessLine(string line, IDataValidator dataValidator, StreamWriter validStream, StreamWriter errorStream)
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
            }
            else if (validationResult is ValidationResult.Error error)
            {
                await errorStream.WriteLineAsync($"{error.Message}\t{line}");
            }
            else
            {
                logger.LogError($"Error validating line, got unknown result {validationResult} ; line = {line}");
            }
        }
        else if (jsonParseResult is JsonParseResult.Error error)
        {
            await errorStream.WriteLineAsync($"{error.Message}\t{line}");
        }
        else
        {
            logger.LogError($"Error parsing line, got unknown result {jsonParseResult} ; line={line}");
        }
    }
}

