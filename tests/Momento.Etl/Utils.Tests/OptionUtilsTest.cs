using System;
using Momento.Etl.Utils;
using Xunit;

namespace Utils.Tests;

public class OptionUtilsTest
{
    [Fact]
    public void TryOpenFile_FileNotFound_ThrowsException()
    {
        Assert.Throws<System.IO.FileNotFoundException>(() => OptionUtils.TryOpenFile(Guid.NewGuid().ToString()));
    }

    [Fact]
    public void AssertStrictlyPositive_NotPositive_Exception()
    {
        Assert.Throws<ArgumentException>(() => OptionUtils.AssertStrictlyPositive(0, "number"));
        Assert.Throws<ArgumentException>(() => OptionUtils.AssertStrictlyPositive(-1, "number"));
    }

    [Fact]
    public void AssertStrictlyPositive_Positive_NoException()
    {
        OptionUtils.AssertStrictlyPositive(1, "name");
    }

    [Fact]
    public void AssertNonnegative_Negative_Exception()
    {
        Assert.Throws<ArgumentException>(() => OptionUtils.AssertNonnegative(-1, "number"));
    }

    [Fact]
    public void AssertNonnegative_Nonnegative_NoException()
    {
        OptionUtils.AssertNonnegative(0, "name");
        OptionUtils.AssertNonnegative(1, "name");
    }

    [Fact]
    public void AssertInUnitInterval_Outside_Exception()
    {
        Assert.Throws<ArgumentException>(() => OptionUtils.AssertInUnitInterval(-0.01, "number"));
        Assert.Throws<ArgumentException>(() => OptionUtils.AssertInUnitInterval(1.01, "number"));
    }

    [Fact]
    public void AssertInUnitInterval_InUnitInterval_NoException()
    {
        OptionUtils.AssertInUnitInterval(0, "name");
        OptionUtils.AssertInUnitInterval(.5, "name");
        OptionUtils.AssertInUnitInterval(1, "name");
    }
}
