using System;
using System.Collections.Generic;
using Momento.Etl.Model;
using Xunit;

namespace Momento.Etl.Model.Tests;

public class RedisSetTest
{
    [Fact]
    public void PayloadSizeInBytes_HasExpiry_HappyPath()
    {
        var item = new RedisSet("key", new List<string>() { "hello", "hola" }, 123);
        Assert.Equal(20, item.ItemSizeInBytes());
    }
}
