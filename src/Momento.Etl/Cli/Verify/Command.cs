using Microsoft.Extensions.Logging;
using Momento.Etl.Model;
using Momento.Sdk.Incubating;
using Momento.Sdk.Incubating.Responses;
using Momento.Sdk.Internal.ExtensionMethods;
using Momento.Sdk.Responses;


namespace Momento.Etl.Cli.Verify;

public class Command : IDisposable
{
    private ILogger logger;
    private SimpleCacheClient client;

    public Command(ILoggerFactory loggerFactory, SimpleCacheClient client)
    {
        logger = loggerFactory.CreateLogger<Command>();
        this.client = client;
    }

    public async Task RunAsync(string cacheName, string filePath)
    {
        logger.LogInformation($"Extracting {filePath} and verifying in Momento");
        using (var stream = File.OpenText(filePath))
        {
            string? line;
            int linesProcessed = 0;
            while ((line = stream.ReadLine()) != null)
            {
                await ProcessLine(cacheName, line);
                linesProcessed++;
                if (linesProcessed % 10_000 == 0)
                {
                    logger.LogInformation($"Processed {linesProcessed}");
                }
            }
        }
        logger.LogInformation("Finished");
    }


    private async Task ProcessLine(string cacheName, string line)
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
            var ttl = item.TtlRelativeToNow();
            if (RedisItem.HasExpiredRelativeToNow(ttl))
            {
                logger.LogInformation($"already_expired: {line}");
                return;
            }
            await Verify(cacheName, item as dynamic, line);
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

    private async Task Verify(string cacheName, RedisString item, string line)
    {
        var response = await client.GetAsync(cacheName, item.Key);
        if (response is CacheGetResponse.Hit hit)
        {
            if (hit.ValueString.Equals(item.Value))
            {
                logger.LogInformation($"{item.Key} (string) - OK");
            }
            else
            {
                logger.LogError($"{item.Key} (string) - ERROR not equal. Expected {item.Value} and got {hit.ValueString}");
            }
        }
        else if (response is CacheGetResponse.Miss miss)
        {
            logger.LogError($"{item.Key} (string) - MISS");
        }
        else if (response is CacheGetResponse.Error error)
        {
            logger.LogError($"error_getting: {error.InnerException.Message}; {line}");
        }
        else
        {
            logger.LogError($"unknown_response: {line}");
        }
    }

    private async Task Verify(string cacheName, RedisHash item, string line)
    {
        var response = await client.DictionaryFetchAsync(cacheName, item.Key);
        if (response is CacheDictionaryFetchResponse.Hit hit)
        {
            if (hit.StringStringDictionary().Count == item.Value.Count && !hit.StringStringDictionary().Except(item.Value).Any())
            {
                logger.LogInformation($"{item.Key} (dictionary) - OK");
            }
            else
            {
                logger.LogError($"{item.Key} (dictionary) - ERROR not equal");
            }
        }
        else if (response is CacheDictionaryFetchResponse.Miss miss)
        {
            logger.LogError($"{item.Key} (dictionary) - MISS");
        }
        else if (response is CacheDictionaryFetchResponse.Error error)
        {
            logger.LogError($"error_getting: {error.Message}; {line}");
        }
        else
        {
            logger.LogError($"unknown_response: {line}");
        }
    }

    private async Task Verify(string cacheName, RedisList item, string line)
    {
        // List operations are not idempotent. Ensure the list is not there.
        var response = await client.ListFetchAsync(cacheName, item.Key);
        if (response is CacheListFetchResponse.Hit hit)
        {
            if (hit.StringList().SequenceEqual(item.Value))
            {
                logger.LogInformation($"{item.Key} (list) - OK");
            }
            else
            {
                logger.LogError($"{item.Key} (list) - ERROR not equal");
            }
        }
        else if (response is CacheListFetchResponse.Miss miss)
        {
            logger.LogError($"{item.Key} (list) - MISS");
        }
        else if (response is CacheListFetchResponse.Error error)
        {
            logger.LogError($"error_getting: {error.Message}; {line}");
        }
        else
        {
            logger.LogError($"unknown_response: {line}");
        }
    }

    private async Task Verify(string cacheName, RedisSet item, string line)
    {
        var response = await client.SetFetchAsync(cacheName, item.Key);
        if (response is CacheSetFetchResponse.Hit hit)
        {
            if (hit.StringSet().SetEquals(item.Value))
            {
                logger.LogInformation($"{item.Key} (set) - OK");
            }
            else
            {
                logger.LogError($"{item.Key} (set) - ERROR not equal");
            }
        }
        else if (response is CacheSetFetchResponse.Miss miss)
        {
            logger.LogError($"{item.Key} (set) - MISS");
        }
        else if (response is CacheSetFetchResponse.Error error)
        {
            logger.LogError($"error_getting: {error.Message}; {line}");
        }
        else
        {
            logger.LogError($"unknown_response: {line}");
        }
    }

#pragma warning disable CS1998
    // disable warnings for no "await"
    private async Task Verify(string cacheName, object item, string line)
    {
        logger.LogError($"unsupported_data_type: {line}");
    }
#pragma warning restore CS1998

    public void Dispose()
    {
        client.Dispose();
    }
}
