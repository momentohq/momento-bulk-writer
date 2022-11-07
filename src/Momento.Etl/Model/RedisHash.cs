using System.Collections.Generic;
using System.Linq;

namespace Momento.Etl.Model;

public record RedisHash : RedisItem
{
    public IDictionary<string, string> Value;

    public RedisHash(string key, IDictionary<string, string> value, long? expiry = null)
    {
        this.Key = key;
        this.Value = value;
        this.Expiry = expiry;
    }

    public override int PayloadSizeInBytes()
    {
        return base.PayloadSizeInBytes() + Value.Sum(item => item.Key.PayloadSizeInBytes() + item.Value.PayloadSizeInBytes());
    }

    public override string ToString()
    {
        return $"RedisHash(key: {Key}, value: {{{String.Join(", ", Value.Select(item => $"{item.Key}: {item.Value}"))}}}, expiry: {Expiry})";
    }
}
