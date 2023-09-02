using System.Collections.Immutable;
using System.Globalization;

namespace SemtexAnalyzers;

// Yeah this is another case of encapsulate ugliness from the rest of the code base. Code Coverage of this file should be pm 100%
public static class ConstantValuePropertyDict
{
    private const string StringValue = "stringValue";
    private const string IntValue = "intValue";
    private const string FloatValue = "floatValue";
    private const string DoubleValue = "doubleValue";

    internal static ImmutableDictionary<string, string?>? GetPropertiesDict(object constantValue)
    {
        return constantValue switch
        {
            string s => new Dictionary<string, string?> { [StringValue] = s }.ToImmutableDictionary(),
            int i => new Dictionary<string, string?> { [IntValue] = i.ToString() }.ToImmutableDictionary(),
            float f => float.IsInfinity(f) || float.IsNaN(f)
                ? null
                : new Dictionary<string, string?> { [FloatValue] = f.ToString(CultureInfo.InvariantCulture) }
                    .ToImmutableDictionary(),
            double d => double.IsInfinity(d) || double.IsNaN(d)
                ? null
                : new Dictionary<string, string?> { [DoubleValue] = d.ToString(CultureInfo.InvariantCulture) }
                    .ToImmutableDictionary(),
            _ => null
        };
    }


    internal static object GetValueFromPropDict(ImmutableDictionary<string, string?> properties)
    {
        if (properties.TryGetValue(StringValue, out var str))
        {
            return str!;
        }
        if (properties.TryGetValue(IntValue, out var intStr))
        {
            return int.Parse(intStr!);
        }
        if (properties.TryGetValue(FloatValue, out var floatStr))
        {
            return float.Parse(floatStr!);
        }
        if (properties.TryGetValue(DoubleValue, out var doubleStr))
        {
            return double.Parse(doubleStr!);
        }

        throw new NotImplementedException();
    }


}