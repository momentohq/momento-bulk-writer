using System;
using System.Collections.Generic;
using System.Text;
using Momento.Etl.Model;
using Momento.Etl.Validation;
using Xunit;

namespace Momento.Etl.Validation.Tests;

public class DataValidatorChainTest
{
    [Fact]
    public void Validate_TrivialList_OK()
    {
        var chain = new DataValidatorChain();
        var result = chain.Validate(new RedisString("hello", "world"));
        Assert.True(result is ValidationResult.OK);
    }

    [Fact]
    public void Validate_ThreeValidators_HappyPath()
    {
        var validators = new List<IDataValidator>()
            { new HasTtlValidator(), new TtlInRangeValidator(TimeSpan.FromHours(1)), new ItemSizeValidator(1) };
        var chain = new DataValidatorChain(validators);

        var result = chain.Validate(new RedisString("hello", "world", DateTime.Now.ToUnixTimeMilliseconds()));
        Assert.True(result is ValidationResult.OK);

        // No TTL
        result = chain.Validate(new RedisString("hello", "world"));
        Assert.True(result is ValidationResult.Error);

        // TTL too long
        result = chain.Validate(new RedisString("hello", "world", (DateTime.Now + TimeSpan.FromHours(2)).ToUnixTimeMilliseconds()));
        Assert.True(result is ValidationResult.Error);

        // Data too big
        var largeString = Utils.RepeatChar('a', 1024 * 1024);
        result = chain.Validate(new RedisString(largeString, largeString, DateTime.Now.ToUnixTimeMilliseconds()));
        Assert.True(result is ValidationResult.Error);
    }
}
