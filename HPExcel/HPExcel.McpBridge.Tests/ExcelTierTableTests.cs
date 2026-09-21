using HPExcel.McpBridge.Safety;
using Xunit;

namespace HPExcel.McpBridge.Tests;

public class ExcelTierTableTests
{
    [Theory]
    [InlineData("Sort", ExcelTier.Write)]
    [InlineData("Insert", ExcelTier.Write)]
    [InlineData("Merge", ExcelTier.Write)]
    [InlineData("UnMerge", ExcelTier.Write)]
    [InlineData("Replace", ExcelTier.Write)]
    [InlineData("PasteSpecial", ExcelTier.Write)]
    [InlineData("Protect", ExcelTier.Write)]
    [InlineData("Unprotect", ExcelTier.Write)]
    [InlineData("Range.Sort", ExcelTier.Write)]
    [InlineData("Range.Insert", ExcelTier.Write)]
    [InlineData("Range.Merge", ExcelTier.Write)]
    [InlineData("Range.UnMerge", ExcelTier.Write)]
    [InlineData("Range.Replace", ExcelTier.Write)]
    [InlineData("Range.PasteSpecial", ExcelTier.Write)]
    public void WriteOperations_ClassifiedAsWrite(string member, ExcelTier expected)
    {
        Assert.Equal(expected, ExcelTierTable.Classify(member));
    }

    [Theory]
    [InlineData("ClearComments", ExcelTier.Destructive)]
    [InlineData("Range.ClearComments", ExcelTier.Destructive)]
    [InlineData("ClearContents", ExcelTier.Destructive)]
    [InlineData("Range.ClearContents", ExcelTier.Destructive)]
    [InlineData("ClearFormats", ExcelTier.Destructive)]
    [InlineData("Delete", ExcelTier.Destructive)]
    [InlineData("Run", ExcelTier.Destructive)]
    public void DestructiveOperations_ClassifiedAsDestructive(string member, ExcelTier expected)
    {
        Assert.Equal(expected, ExcelTierTable.Classify(member));
    }

    [Theory]
    [InlineData("Value", ExcelTier.ReadOnly)]
    [InlineData("Range.Value", ExcelTier.ReadOnly)]
    [InlineData("Formula", ExcelTier.ReadOnly)]
    [InlineData("Address", ExcelTier.ReadOnly)]
    public void ReadOperations_ClassifiedAsReadOnly(string member, ExcelTier expected)
    {
        Assert.Equal(expected, ExcelTierTable.Classify(member));
    }
}
