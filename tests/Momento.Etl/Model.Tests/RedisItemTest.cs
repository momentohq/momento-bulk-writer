using System;
using Momento.Etl.Model;
using Xunit;

namespace Momento.Etl.Model.Tests;

public class RedisItemTest
{
    [Fact]
    public void TtlRelativeToNow_NoTtl_Null()
    {
        var item = new RedisString("key", "value");
        Assert.Null(item.TtlRelativeToNow());
    }

    [Fact]
    public void HasExpiredRelativeToNow_NoTtl_False()
    {
        var item = new RedisString("key", "value");
        Assert.False(item.HasExpiredRelativeToNow());
    }

    [Fact]
    public void FutureExpiries_HappyPath()
    {
        // Set an expiry one hour in the future
        var futureExpiry = DateTime.Now + TimeSpan.FromHours(1);
        var futureExpiryMillis = futureExpiry.ToUnixTimeMilliseconds();
        var item = new RedisString("key", "value", futureExpiryMillis);

        // Calculate the TTL relative to now (ie epsilon time later) 
        var ttl = item.TtlRelativeToNow();
        var delta = TimeSpan.FromHours(1).TotalSeconds - ttl?.TotalSeconds;
        Assert.True(delta >= 0 && delta < 1);

        Assert.False(item.HasExpiredRelativeToNow());
    }

    [Fact]
    public void PastExpiries_HappyPath()
    {
        // Set an expiry one hour in the future
        var pastExpiry = DateTime.Now - TimeSpan.FromHours(1);
        var pastExpiryMillis = pastExpiry.ToUnixTimeMilliseconds();
        var item = new RedisString("key", "value", pastExpiryMillis);

        // Calculate the TTL relative to now (ie epsilon time later) 
        var ttl = item.TtlRelativeToNow();
        Assert.True(ttl < TimeSpan.Zero);

        Assert.True(item.HasExpiredRelativeToNow());
    }
}
