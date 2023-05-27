using FluentAssertions;
using NUnit.Framework;

namespace RoslynCsCodeFixes.UT;

public class TestReflection
{
    [Test]
    public void CheckIfReflectionFails()
    {
        // All this will do is run the static constructors and confirm that all the loading via reflection works.
        RoslynCsCodeFixProviders.SupportedCodeFixes.Should().NotBeEmpty();
    }
}