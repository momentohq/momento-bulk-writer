namespace Momento.Etl.Model;

public abstract record RedisItem
{
    public string Key { get; set; } = default!;
    /// <summary>
    /// Milliseconds since epoch
    /// </summary>
    public long? Expiry { get; set; }

    public TimeSpan? TtlRelativeToNow()
    {
        if (Expiry is null)
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(Expiry.GetValueOrDefault()) - DateTime.Now;
    }

    public bool HasExpiredRelativeToNow()
    {
        var ttl = TtlRelativeToNow();
        return HasExpiredRelativeToNow(ttl);
    }

    public static bool HasExpiredRelativeToNow(TimeSpan? ttl)
    {
        if (ttl is null)
        {
            return false;
        }
        return ttl <= TimeSpan.Zero;
    }

    /// <summary>
    /// An approximate on-the-wire-size of an item.
    /// Used to validate if an item is too large for the cache.
    /// </summary>
    /// <returns></returns>
    public virtual int ItemSizeInBytes()
    {
        // We always include the expiry because Momento requires one, even though Redis does not.
        var sizeOfExpiry = sizeof(long);
        return Key.ItemSizeInBytes() + sizeOfExpiry;
    }
}
