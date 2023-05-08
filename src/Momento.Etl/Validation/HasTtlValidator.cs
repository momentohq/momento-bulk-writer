using Momento.Etl.Model;

namespace Momento.Etl.Validation;

/// <summary>
/// Tests whether an item has a TTL.
/// </summary>
public class HasTtlValidator : IDataValidator
{
    public ValidationResult Validate(RedisItem item)
    {
        if (item.Expiry.HasValue)
        {
            return ValidationResult.OK.Instance;
        }
        else
        {
            return new ValidationResult.Error(ErrorMessage.NO_TTL);
        }
    }
}
