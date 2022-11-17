using System;
using Momento.Etl.Model;
using Xunit;

namespace Momento.Etl.Model.Tests;

public class RedisStringTest
{
    [Fact]
    public void PayloadSizeInBytes_HasExpiry_HappyPath()
    {
        var item = new RedisString("key", "value", 123);
        Assert.Equal(16, item.ItemSizeInBytes());
    }
}
