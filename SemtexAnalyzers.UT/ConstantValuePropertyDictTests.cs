using FluentAssertions;
using SemtexAnalyzers;

namespace SemtexAnalyzers.UT;

public class ConstantValuePropertyDictTests
{
    [Test]
    public void Test_StringInStringOut()
    {
        const string s = "Some String";
        var propDict = ConstantValuePropertyDict.GetPropertiesDict(s);
        propDict.Should().NotBeNull();
        var val = ConstantValuePropertyDict.GetValueFromPropDict(propDict!);
        val.Should().Be(s);
    }

    [Test]
    public void Test_IntInIntOut()
    {
        const int i = 3;
        var propDict = ConstantValuePropertyDict.GetPropertiesDict(i);
        propDict.Should().NotBeNull();
        var val = ConstantValuePropertyDict.GetValueFromPropDict(propDict!);
        val.Should().Be(i);
    }

    [Test]
    public void Test_FloatInFloatOut()
    {
        const float f = 3;
        var propDict = ConstantValuePropertyDict.GetPropertiesDict(f);
        propDict.Should().NotBeNull();
        var val = ConstantValuePropertyDict.GetValueFromPropDict(propDict!);
        val.Should().Be(f);
    }

    [Test]
    public void Test_DoubleInDoubleOut()
    {
        const int d = 3;
        var propDict = ConstantValuePropertyDict.GetPropertiesDict(d);
        propDict.Should().NotBeNull();
        var val = ConstantValuePropertyDict.GetValueFromPropDict(propDict!);
        val.Should().Be(d);
    }
    [Test]
    public void Test_UnknownTypeShouldReturnNullPropDict()
    {
        object o = new();
        var propDict = ConstantValuePropertyDict.GetPropertiesDict(o);
        propDict.Should().BeNull();
    }
}