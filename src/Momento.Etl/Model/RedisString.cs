namespace Momento.Etl.Model;

public record RedisString : RedisItem
{
    public string Value;
    public RedisString(string key, string value, long? expiry = null)
    {
        this.Key = key;
        this.Value = value;
        this.Expiry = expiry;
    }

    public override int PayloadSizeInBytes()
    {
        return base.PayloadSizeInBytes() + Value.PayloadSizeInBytes();
    }

    public override string ToString()
    {
        return $"RedisString(key: {Key}, value: {Value}, expiry: {Expiry})";
    }
}
