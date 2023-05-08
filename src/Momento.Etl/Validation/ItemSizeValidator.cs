using Momento.Etl.Model;

namespace Momento.Etl.Validation;

/// <summary>
/// Tests whether an item size is within the max limit.
/// </summary>
public class ItemSizeValidator : IDataValidator
{
    private readonly int maxSizeInBytes;

    public ItemSizeValidator(int maxSizeInMiB)
    {
        maxSizeInBytes = maxSizeInMiB * 1024 * 1024;
    }

    public ValidationResult Validate(RedisItem item)
    {
        if (item.ItemSizeInBytes() <= maxSizeInBytes)
        {
            return ValidationResult.OK.Instance;
        }
        else
        {
            return new ValidationResult.Error(ErrorMessage.DATA_TOO_LARGE);
        }
    }
}
