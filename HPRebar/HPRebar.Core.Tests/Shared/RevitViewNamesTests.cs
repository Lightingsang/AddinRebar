using HPRebar.Core.Shared;
using Xunit;

namespace HPRebar.Core.Tests.Shared;

public sealed class RevitViewNamesTests
{
    [Theory]
    [InlineData("Beam: Detail", ':')]
    [InlineData("Sec[1]", '[')]
    [InlineData("A~B", '~')]
    public void TryFindForbiddenCharacter_NameRevitRefuses_ReturnsFirstOffendingCharacter(string name, char expected)
    {
        Assert.True(RevitViewNames.TryFindForbiddenCharacter(name, out var found));
        Assert.Equal(expected, found);
    }

    [Theory]
    [InlineData("Beam Detail - Span 1 - Sec 2")]
    [InlineData("Dầm D1 (trục A-B)")]
    [InlineData("")]
    public void TryFindForbiddenCharacter_AcceptedName_ReturnsFalse(string name)
    {
        Assert.False(RevitViewNames.TryFindForbiddenCharacter(name, out _));
    }
}
