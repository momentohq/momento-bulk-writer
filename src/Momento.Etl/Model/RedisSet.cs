namespace Momento.Etl.Model;

public record RedisSet : RedisItem
{
    public IEnumerable<string> Value;
    public RedisSet(string key, IEnumerable<string> value, long? expiry = null)
    {
        this.Key = key;
        this.Value = value;
        this.Expiry = expiry;
    }

    public override int ItemSizeInBytes()
    {
        return base.ItemSizeInBytes() + Value.Sum(item => item.ItemSizeInBytes());
    }

    public override string ToString()
    {
        return $"RedisSet(key: {Key}, value: {{{String.Join(", ", Value)}}}, expiry: {Expiry})";
    }
}
