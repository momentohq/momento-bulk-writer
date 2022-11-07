using Momento.Etl.Cli;
using Momento.Etl.Model;
using Momento.Etl.Validation;
using Xunit;

namespace Momento.Etl.Cli.Tests;

public class RdbJsonReaderTest
{
    [Fact]
    public void ParseJson_OK_HappyPath()
    {
        var json = "{ \"key\": \"hello\", \"value\": \"world\", \"type\": \"string\", \"db\": 0 }";
        var result = RdbJsonReader.ParseJson(json);
        Assert.True(result is JsonParseResult.OK, $"result should be OK, was {result.GetType().Name}");

        var ok = (JsonParseResult.OK)result;
        Assert.True(ok.Item is RedisString, $"item should be RedisString, was {result.GetType().Name}");

        var item = (RedisString)ok.Item;
        Assert.Equal("hello", item.Key);
        Assert.Equal("world", item.Value);
    }

    [Theory]
    [InlineData("{ ")]
    [InlineData("{ \"key\": \"hello\", \"value\": \"world\" }")]
    public void ParseJson_InvalidJson_Error(string json)
    {
        var result = RdbJsonReader.ParseJson(json);

        Assert.True(result is JsonParseResult.Error, $"result should be error, was {result.GetType().Name}");
        JsonParseResult.Error error = (JsonParseResult.Error)result;
        Assert.Equal(ErrorMessage.INVALID_JSON, error.Message);
    }

    [Theory]
    [InlineData("{ \"key\": \"hello\", \"value\": \"world\", \"type\": 0 }")]
    [InlineData("{ \"key\": \"hello\", \"value\": \"world\", \"type\": \"sortedset\" }")]
    public void ParseJson_DataTypeNotSupported_Error(string json)
    {
        var result = RdbJsonReader.ParseJson(json);

        Assert.True(result is JsonParseResult.Error, $"result should be error, was {result.GetType().Name}");
        JsonParseResult.Error error = (JsonParseResult.Error)result;
        Assert.Equal(ErrorMessage.DATA_TYPE_NOT_SUPPORTED, error.Message);
    }
}
