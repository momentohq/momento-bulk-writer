using System;
using Momento.Etl.Model;
using Momento.Etl.Validation;
using Xunit;

namespace Momento.Etl.Validation.Tests;

public class TtlInRangeValidatorTest
{
    [Fact]
    public void Validate_NoTtl_OK()
    {
        var validator = new TtlInRangeValidator(maxTtl: TimeSpan.FromHours(1));
        var result = validator.Validate(new RedisString("hello", "world"));
        Assert.True(result is ValidationResult.OK);
    }

    [Fact]
    public void Validate_TtlLessThanMax_OK()
    {
        var validator = new TtlInRangeValidator(maxTtl: TimeSpan.FromHours(1));
        var expiry = DateTime.Now.ToUnixTimeMilliseconds();
        var result = validator.Validate(new RedisString("hello", "world", expiry));
        Assert.True(result is ValidationResult.OK);
    }

    [Fact]
    public void Validate_TtlLessThanMax_IsError()
    {
        var validator = new TtlInRangeValidator(maxTtl: TimeSpan.FromHours(1));
        var expiry = (DateTime.Now + TimeSpan.FromHours(2)).ToUnixTimeMilliseconds();
        var result = validator.Validate(new RedisString("hello", "world", expiry));
        Assert.True(result is ValidationResult.Error);
        var error = (ValidationResult.Error)result;
        Assert.Equal(ErrorMessage.TTL_TOO_LONG, error.Message);
    }
}
