using Momento.Etl.Model;
using Momento.Etl.Validation;
using Xunit;

namespace Momento.Etl.Validation.Tests;

public class HasTtlValidatorTest
{
    [Fact]
    public void Validate_TtlPresent_OK()
    {
        var validator = new HasTtlValidator();
        var result = validator.Validate(new RedisString("hello", "world", 123));
        Assert.True(result is ValidationResult.OK);
    }

    [Fact]
    public void Validate_TtlMissing_Error()
    {
        var validator = new HasTtlValidator();
        var result = validator.Validate(new RedisString("hello", "world"));
        Assert.True(result is ValidationResult.Error);
        ValidationResult.Error error = (ValidationResult.Error)result;
        Assert.Equal(ErrorMessage.NO_TTL, error.Message);
    }
}
