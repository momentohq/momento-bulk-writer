using Momento.Etl.Model;

namespace Momento.Etl.Validation;

/// <summary>
/// Tests whether an item has already expired.
/// </summary>
public class HasntAlreadyExpiredValidator : IDataValidator
{
    public ValidationResult Validate(RedisItem item)
    {
        if (!item.HasExpiredRelativeToNow())
        {
            return ValidationResult.OK.Instance;
        }
        else
        {
            return new ValidationResult.Error(ErrorMessage.ALREADY_EXPIRED);
        }
    }
}
