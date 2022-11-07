using Momento.Etl.Model;

namespace Momento.Etl.Validation;


public interface IDataValidator
{
    ValidationResult Validate(RedisItem item);
}
