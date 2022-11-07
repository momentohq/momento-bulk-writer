using Momento.Etl.Model;

namespace Momento.Etl.Validation;

/// <summary>
/// Runs multiple validators in series.
/// </summary>
public class DataValidatorChain : IDataValidator
{
    private List<IDataValidator> dataValidators = new();

    public DataValidatorChain()
    {

    }

    public DataValidatorChain(IEnumerable<IDataValidator> dataValidators)
    {
        foreach (var dataValidator in dataValidators)
        {
            AddDataValidator(dataValidator);
        }
    }
    public void AddDataValidator(IDataValidator dataValidator)
    {
        dataValidators.Add(dataValidator);
    }

    public ValidationResult Validate(RedisItem item)
    {
        foreach (var dataValidator in dataValidators)
        {
            var result = dataValidator.Validate(item);
            if (result is ValidationResult.Error)
            {
                return result;
            }
        }
        return ValidationResult.OK.Instance;
    }
}
