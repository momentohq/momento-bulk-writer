using Momento.Etl.Model;
namespace Momento.Etl.Validation;

/// <summary>
/// Tests if a TTL is within limits, ie not too long.
/// </summary>
public class TtlInRangeValidator : IDataValidator
{
    private TimeSpan maxTtl;
    public TtlInRangeValidator(TimeSpan maxTtl)
    {
        this.maxTtl = maxTtl;
    }

    public ValidationResult Validate(RedisItem item)
    {
        var ttl = item.TtlRelativeToNow();
        if (ttl is null || item.TtlRelativeToNow() <= maxTtl)
        {
            return ValidationResult.OK.Instance;
        }

        return new ValidationResult.Error(ErrorMessage.TTL_TOO_LONG);
    }
}
