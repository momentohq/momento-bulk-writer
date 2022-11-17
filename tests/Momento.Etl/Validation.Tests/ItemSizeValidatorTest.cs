using System.Text;
using Momento.Etl.Model;
using Momento.Etl.Validation;
using Xunit;

namespace Momento.Etl.Validation.Tests;

public class ItemSizeValidatorTest
{
    [Fact]
    public void Validate_InLimits_OK()
    {
        var validator = new ItemSizeValidator(1);
        var result = validator.Validate(new RedisString("hello", "world", 123));
        Assert.True(result is ValidationResult.OK, $"result was not OK, instead was {result.GetType().Name}");
    }

    [Fact]
    public void Validate_AsBigAsPossible_OK()
    {

        StringBuilder sb = new StringBuilder();
        // Since the expiry is 8 bytes, we make the key and value large enough for it all to equal 1 MiB
        var largeString = Utils.RepeatChar('a', (1024 * 1024) / 2 - 4);
        var item = new RedisString(largeString, largeString);
        Assert.Equal(1024 * 1024, item.ItemSizeInBytes());

        // Test max size inclusive
        var validator = new ItemSizeValidator(1);
        var result = validator.Validate(item);
        Assert.True(result is ValidationResult.OK);
    }

    [Fact]
    public void Validate_TooBig_Error()
    {
        StringBuilder sb = new StringBuilder();
        // Since the expiry is 9 bytes, we make the key and value 1MiB + 1 byte
        var largeString1 = Utils.RepeatChar('a', (1024 * 1024) / 2 - 4);
        var largeString2 = largeString1 + 'a';
        var item = new RedisString(largeString1, largeString2);
        Assert.Equal(1024 * 1024 + 1, item.ItemSizeInBytes());

        // Test max size inclusive
        var validator = new ItemSizeValidator(1);
        var result = validator.Validate(item);
        Assert.True(result is ValidationResult.Error, $"result wasn't Error, instead was {result.GetType().Name}");
        ValidationResult.Error error = (ValidationResult.Error)result;
        Assert.Equal(ErrorMessage.DATA_TOO_LARGE, error.Message);
    }
}
