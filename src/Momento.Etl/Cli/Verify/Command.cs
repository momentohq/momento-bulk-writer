using Microsoft.Extensions.Logging;
using Momento.Etl.Model;
using Momento.Sdk;
using Momento.Sdk.Internal.ExtensionMethods;
using Momento.Sdk.Responses;


namespace Momento.Etl.Cli.Verify;

public class Command : IDisposable
{
    private ILogger logger;
    private ICacheClient client;

    private static int BUFFER_SIZE = 1024;

    public Command(ILoggerFactory loggerFactory, ICacheClient client)
    {
        logger = loggerFactory.CreateLogger<Command>();
        this.client = client;
    }

    public async Task RunAsync(string cacheName, string filePath, int numberOfConcurrentRequests = 1)
    {
        logger.LogInformation($"Extracting {filePath} and verifying in Momento with a max concurrency of {numberOfConcurrentRequests}");
        var numProcessed = 0;
        var numErrors = 0;

        BUFFER_SIZE = Math.Max(BUFFER_SIZE, numberOfConcurrentRequests);
        var workBuffer = new List<string>(BUFFER_SIZE);

        using (var stream = File.OpenText(filePath))
        {
            string? line;
            while ((line = stream.ReadLine()?.Trim()) != null)
            {
                if (line.Equals(""))
                {
                    continue;
                }
                workBuffer.Add(line);
                if (workBuffer.Count == BUFFER_SIZE)
                {
                    numErrors += await ProcessWorkBuffer(cacheName, workBuffer, numberOfConcurrentRequests);
                }

                numProcessed++;
                if (numProcessed % 10_000 == 0)
                {
                    logger.LogInformation($"Processed {numProcessed}");
                }
            }
            if (workBuffer.Count > 0)
            {
                numErrors += await ProcessWorkBuffer(cacheName, workBuffer, numberOfConcurrentRequests);
            }
        }
        logger.LogInformation($"Finished: {numErrors} errors in {numProcessed} items");
    }

    private async Task<int> ProcessWorkBuffer(string cacheName, List<string> workBuffer, int numberOfConcurrentRequests)
    {
        var numErrors = 0;
        await Parallel.ForEachAsync(
            workBuffer,
            new ParallelOptions { MaxDegreeOfParallelism = numberOfConcurrentRequests },
            async (line, ct) =>
            {
                var ok = await ProcessLine(cacheName, line);
                if (!ok)
                {
                    Interlocked.Increment(ref numErrors);
                }
            });
        workBuffer.Clear();
        return numErrors;
    }

    private async Task<bool> ProcessLine(string cacheName, string line)
    {
        var result = RdbJsonReader.ParseJson(line);
        if (result is JsonParseResult.OK ok)
        {
            var item = ok.Item;
            return await Verify(cacheName, item as dynamic, line);
        }
        else if (result is JsonParseResult.Error error)
        {
            logger.LogError($"{error.Message}: {line}");
        }
        else
        {
            logger.LogError($"should not reach here: {line}");
        }
        return false;
    }

    private async Task<bool> Verify(string cacheName, RedisString item, string line)
    {
        var response = await client.GetAsync(cacheName, item.Key);
        if (response is CacheGetResponse.Hit hit)
        {
            if (hit.ValueString.Equals(item.Value))
            {
                logger.LogDebug($"{item.Key} (string) - OK");
                return true;
            }
            else
            {
                logger.LogError($"{item.Key} (string) - ERROR not equal. Expected {item.Value} and got {hit.ValueString}");
            }
        }
        else if (response is CacheGetResponse.Miss miss)
        {
            if (item.HasExpiredRelativeToNow())
            {
                logger.LogDebug($"{item.Key} (string) - OK - expired");
                return true;
            }
            else
            {
                logger.LogError($"{item.Key} (string) - MISS");
            }
        }
        else if (response is CacheGetResponse.Error error)
        {
            logger.LogError($"error_getting: {error.InnerException.Message}; {line}");
        }
        else
        {
            logger.LogError($"unknown_response: {line}");
        }
        return false;
    }

    private async Task<bool> Verify(string cacheName, RedisHash item, string line)
    {
        var response = await client.DictionaryFetchAsync(cacheName, item.Key);
        if (response is CacheDictionaryFetchResponse.Hit hit)
        {
            if (hit.ValueDictionaryStringString.Count == item.Value.Count && !hit.ValueDictionaryStringString.Except(item.Value).Any())
            {
                logger.LogDebug($"{item.Key} (dictionary) - OK");
                return true;
            }
            else
            {
                logger.LogError($"{item.Key} (dictionary) - ERROR not equal");
            }
        }
        else if (response is CacheDictionaryFetchResponse.Miss miss)
        {
            if (item.HasExpiredRelativeToNow())
            {
                logger.LogDebug($"{item.Key} (dictionary) - OK - expired");
                return true;
            }
            else
            {
                logger.LogError($"{item.Key} (dictionary) - MISS");
            }
        }
        else if (response is CacheDictionaryFetchResponse.Error error)
        {
            logger.LogError($"error_getting: {error.Message}; {line}");
        }
        else
        {
            logger.LogError($"unknown_response: {line}");
        }
        return false;
    }

    private async Task<bool> Verify(string cacheName, RedisList item, string line)
    {
        // List operations are not idempotent. Ensure the list is not there.
        var response = await client.ListFetchAsync(cacheName, item.Key);
        if (response is CacheListFetchResponse.Hit hit)
        {
            if (hit.ValueListString.SequenceEqual(item.Value))
            {
                logger.LogDebug($"{item.Key} (list) - OK");
                return true;
            }
            else
            {
                logger.LogError($"{item.Key} (list) - ERROR not equal");
            }
        }
        else if (response is CacheListFetchResponse.Miss miss)
        {
            if (item.HasExpiredRelativeToNow())
            {
                logger.LogDebug($"{item.Key} (list) - OK - expired");
                return true;
            }
            else
            {
                logger.LogError($"{item.Key} (list) - MISS");
            }
        }
        else if (response is CacheListFetchResponse.Error error)
        {
            logger.LogError($"error_getting: {error.Message}; {line}");
        }
        else
        {
            logger.LogError($"unknown_response: {line}");
        }
        return false;
    }

    private async Task<bool> Verify(string cacheName, RedisSet item, string line)
    {
        var response = await client.SetFetchAsync(cacheName, item.Key);
        if (response is CacheSetFetchResponse.Hit hit)
        {
            if (hit.ValueSetString.SetEquals(item.Value))
            {
                logger.LogDebug($"{item.Key} (set) - OK");
                return true;
            }
            else
            {
                logger.LogError($"{item.Key} (set) - ERROR not equal");
            }
        }
        else if (response is CacheSetFetchResponse.Miss miss)
        {
            if (item.HasExpiredRelativeToNow())
            {
                logger.LogDebug($"{item.Key} (set) - OK - expired");
                return true;
            }
            else
            {
                logger.LogError($"{item.Key} (set) - MISS");
            }
        }
        else if (response is CacheSetFetchResponse.Error error)
        {
            logger.LogError($"error_getting: {error.Message}; {line}");
        }
        else
        {
            logger.LogError($"unknown_response: {line}");
        }
        return false;
    }

#pragma warning disable CS1998
    // disable warnings for no "await"
    private async Task<bool> Verify(string cacheName, object item, string line)
    {
        logger.LogError($"unsupported_data_type: {line}");
        return false;
    }
#pragma warning restore CS1998

    public void Dispose()
    {
        client.Dispose();
    }
}
