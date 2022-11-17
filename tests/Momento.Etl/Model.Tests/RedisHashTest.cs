using System;
using System.Collections.Generic;
using Momento.Etl.Model;
using Xunit;

namespace Momento.Etl.Model.Tests;

public class RedisHashTest
{
    [Fact]
    public void PayloadSizeInBytes_HasExpiry_HappyPath()
    {
        var item = new RedisHash("key", new Dictionary<string, string>() { { "hello", "hola" }, { "goodbye", "adios" } }, 123);
        Assert.Equal(32, item.ItemSizeInBytes());
    }
}
