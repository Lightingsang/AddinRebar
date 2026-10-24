using HPBimToolkit.Core.Contracts;
using HPBimToolkit.Core.Domain.Dashboard;
using Xunit;

namespace HPBimToolkit.Core.Tests.Domain;

public sealed class ShowElementsRulesTests
{
    [Fact]
    public void SourceOf_CurrentSelection_WinsOverIdsAndCategory()
    {
        var request = new ShowElementsRequest(CategoryId: -2000011, Ids: [1, 2], CurrentSelection: true);

        Assert.Equal(ShowElementsSource.CurrentSelection, ShowElementsRules.SourceOf(request));
    }

    [Fact]
    public void SourceOf_CurrentSelection_IgnoresAnOversizedIdList()
    {
        var ids = Enumerable.Range(1, ShowElementsRules.MaxRequestedIds + 1).Select(id => (long)id).ToArray();

        Assert.Equal(ShowElementsSource.CurrentSelection, ShowElementsRules.SourceOf(new ShowElementsRequest(Ids: ids, CurrentSelection: true)));
    }

    [Fact]
    public void SourceOf_CategoryWithoutIds_IsCategory()
    {
        Assert.Equal(ShowElementsSource.Category, ShowElementsRules.SourceOf(new ShowElementsRequest(CategoryId: -2000011)));
    }

    [Fact]
    public void SourceOf_Ids_WinOverCategory()
    {
        Assert.Equal(ShowElementsSource.Ids, ShowElementsRules.SourceOf(new ShowElementsRequest(CategoryId: -2000011, Ids: [7])));
    }

    [Fact]
    public void SourceOf_EmptyIdsWithCategory_IsCategory()
    {
        Assert.Equal(ShowElementsSource.Category, ShowElementsRules.SourceOf(new ShowElementsRequest(CategoryId: -2000011, Ids: [])));
    }

    [Fact]
    public void SourceOf_IdsAtTheLimit_AreAccepted()
    {
        var ids = Enumerable.Range(1, ShowElementsRules.MaxRequestedIds).Select(id => (long)id).ToArray();

        Assert.Equal(ShowElementsSource.Ids, ShowElementsRules.SourceOf(new ShowElementsRequest(Ids: ids)));
    }

    [Fact]
    public void SourceOf_TooManyIds_IsBadRequest()
    {
        var ids = Enumerable.Range(1, ShowElementsRules.MaxRequestedIds + 1).Select(id => (long)id).ToArray();

        var error = Assert.Throws<BridgeException>(() => ShowElementsRules.SourceOf(new ShowElementsRequest(Ids: ids)));
        Assert.Equal(BridgeErrorCodes.BadRequest, error.Code);
    }

    [Fact]
    public void SourceOf_NothingNamed_IsBadRequest()
    {
        var error = Assert.Throws<BridgeException>(() => ShowElementsRules.SourceOf(new ShowElementsRequest()));
        Assert.Equal(BridgeErrorCodes.BadRequest, error.Code);
    }
}
