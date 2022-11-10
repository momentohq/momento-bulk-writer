using Microsoft.Extensions.Logging;
using Momento.Etl.Model;
using Momento.Sdk.Incubating;
using Momento.Sdk.Incubating.Responses;
using Momento.Sdk.Responses;


namespace Momento.Etl.Cli.Load;

public class Command : IDisposable
{
    private ILogger logger;
    private SimpleCacheClient client;
    private bool createCache;

    public Command(ILoggerFactory loggerFactory, SimpleCacheClient client, bool createCache)
    {
        logger = loggerFactory.CreateLogger<Command>();
        this.client = client;
        this.createCache = createCache;
    }

    public async Task RunAsync(string cacheName, string filePath, TimeSpan maxTtl, bool resetAlreadyExpiredToDefaultTtl = false)
    {
        if (createCache)
        {
            await CreateCacheAsync(cacheName);
        }

        // NB: This is false by default. We turn this on for testing:
        // when using an aging snapshot, items will eventually expire.
        // If we always throw out expired items, then a snapshot will eventually
        // be useless. Hence for testing we will probably use this, but not
        // for live migrations.
        if (resetAlreadyExpiredToDefaultTtl)
        {
            logger.LogInformation($"Resetting already expired items to use the default TTL");
        }
        logger.LogInformation($"Extracting {filePath} and loading into Momento");
        using (var stream = File.OpenText(filePath))
        {
            string? line;
            int linesProcessed = 0;
            while ((line = stream.ReadLine()) != null)
            {
                await ProcessLine(cacheName, line, maxTtl, resetAlreadyExpiredToDefaultTtl);
                linesProcessed++;
                if (linesProcessed % 10_000 == 0)
                {
                    logger.LogInformation($"Processed {linesProcessed}");
                }
            }
        }
        logger.LogInformation("Finished");
    }


    private async Task ProcessLine(string cacheName, string line, TimeSpan maxTtl, bool resetAlreadyExpiredToDefaultTtl = false)
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
                if (resetAlreadyExpiredToDefaultTtl)
                {
                    // The client will use the default TTL
                    ttl = null;
                }
                else
                {
                    logger.LogInformation($"already_expired: {line}");
                    return;
                }
            }
            else if (ttl > maxTtl)
            {
                logger.LogInformation($"clipping_ttl: {line}");
                ttl = maxTtl;
            }
            await Load(cacheName, item as dynamic, ttl, line);
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

    private async Task Load(string cacheName, RedisString item, TimeSpan? ttl, string line)
    {
        var response = await client.SetAsync(cacheName, item.Key, item.Value, ttl);
        if (response is CacheSetResponse.Success)
        {
            // success is a no-op. we include this branch for pattern-matching completeness
        }
        else if (response is CacheSetResponse.Error error)
        {
            logger.LogError($"error_storing: {error.InnerException.Message}; {line}");
        }
        else
        {
            logger.LogError($"unknown_response: {line}");
        }
    }

    private async Task Load(string cacheName, RedisHash item, TimeSpan? ttl, string line)
    {
        var response = await client.DictionarySetBatchAsync(cacheName, item.Key, item.Value, true, ttl);
        if (response is CacheDictionarySetBatchResponse.Success)
        {
            // success is a no-op. we include this branch for pattern-matching completeness
        }
        else if (response is CacheDictionarySetBatchResponse.Error error)
        {
            logger.LogError($"error_storing: {error.Message}; {line}");
        }
        else
        {
            logger.LogError($"unknown_response: {line}");
        }
    }

    private async Task Load(string cacheName, RedisList item, TimeSpan? ttl, string line)
    {
        // List operations are not idempotent. Ensure the list is not there.
        var deleteResponse = await client.ListDeleteAsync(cacheName, item.Key);
        if (deleteResponse is CacheListDeleteResponse.Success)
        {
            // success is a no-op. we include this branch for pattern-matching completeness
        }
        else if (deleteResponse is CacheListDeleteResponse.Error error)
        {
            logger.LogError($"error_deleting: {error.Message}; {line}");
        }
        else
        {
            logger.LogError($"unknown_response: {line}");
        }

        foreach (var listItem in item.Value)
        {
            var pushResponse = await client.ListPushBackAsync(cacheName, item.Key, listItem, true, ttl);
            if (pushResponse is CacheListPushBackResponse.Success)
            {
                // success is a no-op. we include this branch for pattern-matching completeness
            }
            else if (pushResponse is CacheListPushBackResponse.Error pushError)
            {
                logger.LogError($"error_storing: {pushError.Message}; {line}");
            }
            else
            {
                logger.LogError($"unknown_response: {line}");
            }
        }
    }

    private async Task Load(string cacheName, RedisSet item, TimeSpan? ttl, string line)
    {
        var response = await client.SetAddBatchAsync(cacheName, item.Key, item.Value, true, ttl);
        if (response is CacheSetAddBatchResponse.Error error)
        {
            logger.LogError($"error_storing: {error.Message}; {line}");
        }
        else
        {
            logger.LogError($"unknown_response: {line}");
        }
    }

#pragma warning disable CS1998
    // disable warnings for no "await"
    private async Task Load(string cacheName, object item, TimeSpan? ttl, string line)
    {
        logger.LogError($"unsupported_data_type: {line}");
    }
#pragma warning restore CS1998

    private async Task CreateCacheAsync(string cacheName)
    {
        logger.LogInformation($"Initializing Momento with cacheName: {cacheName}");

        logger.LogInformation($"Creating {cacheName} if missing");
        var createCacheResponse = await client.CreateCacheAsync(cacheName);
        if (createCacheResponse is CreateCacheResponse.Success success)
        {
            logger.LogInformation($"{cacheName} created");
        }
        else if (createCacheResponse is CreateCacheResponse.CacheAlreadyExists alreadyExists)
        {
            logger.LogInformation($"{cacheName} already exists. No need to create it");
        }
        else if (createCacheResponse is CreateCacheResponse.Error error)
        {
            logger.LogError($"Error creating cache: {error.Message}");
            await ExitUtils.DelayedExit(1);
        }
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
