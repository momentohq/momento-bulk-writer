using System.Linq;

namespace Momento.Etl.Model;

public record RedisList : RedisItem
{
    public IEnumerable<string> Value;
    public RedisList(string key, IEnumerable<string> value, long? expiry = null)
    {
        this.Key = key;
        this.Value = value;
        this.Expiry = expiry;
    }

    public override int PayloadSizeInBytes()
    {
        return base.PayloadSizeInBytes() + Value.Sum(item => item.PayloadSizeInBytes());
    }

    public override string ToString()
    {
        return $"RedisList(key: {Key}, [{String.Join(",", Value)}], expiry: {Expiry})";
    }
}
