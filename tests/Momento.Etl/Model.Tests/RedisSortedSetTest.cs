using System;
using System.Collections.Generic;
using Momento.Etl.Model;
using Xunit;

namespace Momento.Etl.Model.Tests;

public class RedisSortedSetTest
{
    [Fact]
    public void PayloadSizeInBytes_HasExpiry_HappyPath()
    {
        var item = new RedisSortedSet("key", new Dictionary<string, double>() { { "taylor", 1.23 }, { "alexa", 665.12 } }, 123);
        Assert.Equal(38, item.ItemSizeInBytes());
    }
}
