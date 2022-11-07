using System;
using Momento.Etl.Model;
using Momento.Etl.Validation;
using Xunit;

namespace Momento.Etl.Validation.Tests;

public class HasntAlreadyExpiredValidatorTest
{
    [Fact]
    public void Validate_TtlPresentNotExpired_OK()
    {
        var validator = new HasntAlreadyExpiredValidator();
        var result = validator.Validate(new RedisString("hello", "world", (DateTime.Now + TimeSpan.FromHours(1)).ToUnixTimeMilliseconds()));
        Assert.True(result is ValidationResult.OK, $"result should be OK, was ${result.GetType().Name}");
    }

    [Fact]
    public void Validate_TtlMissing_OK()
    {
        var validator = new HasntAlreadyExpiredValidator();
        var result = validator.Validate(new RedisString("hello", "world"));
        Assert.True(result is ValidationResult.OK, $"result should be OK, was ${result.GetType().Name}");
    }

    [Fact]
    public void Validate_TtlPresentAlreadyExpired_Error()
    {
        var validator = new HasntAlreadyExpiredValidator();
        var result = validator.Validate(new RedisString("hello", "world", (DateTime.Now - TimeSpan.FromHours(1)).ToUnixTimeMilliseconds()));
        Assert.True(result is ValidationResult.Error, $"result should be error, was ${result.GetType().Name}");
        var error = (ValidationResult.Error)result;
        Assert.Equal(ErrorMessage.ALREADY_EXPIRED, error.Message);
    }
}
