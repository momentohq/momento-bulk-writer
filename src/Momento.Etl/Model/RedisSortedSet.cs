using System;
using System.Collections.Generic;
using System.Linq;

namespace Momento.Etl.Model;

public record RedisSortedSet : RedisItem
{
    public IDictionary<string, double> Value { get; init; }

    public RedisSortedSet(string key, IDictionary<string, double> value, long? expiry = null)
    {
        this.Key = key;
        this.Value = value;
        this.Expiry = expiry;
    }

    public override int ItemSizeInBytes()
    {
        int baseSize = base.ItemSizeInBytes();
        int valueSize = Value.Sum(pair => pair.Key.ItemSizeInBytes() + sizeof(double));
        return baseSize + valueSize;
    }

    public override string ToString()
    {
        var valuesString = string.Join(", ", Value.Select(pair => $"{pair.Key}: {pair.Value}"));
        return $"RedisSortedSet(key: {Key}, value: {{{valuesString}}}, expiry: {Expiry})";
    }
}
