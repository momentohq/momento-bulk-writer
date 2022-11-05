using Momento.Import.Rdb.RedisLoadGenerator;
using Xunit;

namespace Momento.Import.Rdb.RedisLoadGenerator.Tests;

public class DataGeneratorTest
{
    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    public void RandomishString_Size_HappyPath(int size)
    {
        var dataGenerator = new DataGenerator(1000, 1, .25);
        var randomishString = dataGenerator.RandomishString(size);
        Assert.Equal(size, randomishString.Length);
    }

    [Fact]
    public void Randomish1KBString_Size_HappyPath()
    {
        var dataGenerator = new DataGenerator(1000, 1, .25);
        var randomishString = dataGenerator.Randomish1KBString();
        Assert.Equal(1024, randomishString.Length);
    }
}
