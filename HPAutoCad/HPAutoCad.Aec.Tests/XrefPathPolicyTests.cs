using HPAutoCad.Aec.Cad;
using Xunit;

namespace HPAutoCad.Aec.Tests;

public sealed class XrefPathPolicyTests
{
    [Theory]
    [InlineData(@"D:\Projects\Tower\A-L01.dwg")]
    [InlineData("D:/Projects/Tower/A-L01.DWG")]
    [InlineData(@"\\server\share\bg\A-L01.dwg")]
    public void Fully_qualified_dwg_paths_are_accepted(string path)
    {
        Assert.Null(XrefPathPolicy.Refuse(path));
    }

    [Theory]
    [InlineData("", "attach needs path")]
    [InlineData("C:x.dwg", "not fully qualified")]
    [InlineData(@"\x.dwg", "not fully qualified")]
    [InlineData(@"..\x.dwg", "not fully qualified")]
    [InlineData(@"D:\a\..\x.dwg", "'..' segments")]
    [InlineData(@"D:\a\x.dxf", "not a .dwg")]
    [InlineData(@"D:\a\x", "not a .dwg")]
    public void Relative_drive_relative_parent_and_non_dwg_paths_are_refused(string path, string reason)
    {
        var refusal = XrefPathPolicy.Refuse(path);

        Assert.NotNull(refusal);
        Assert.Contains(reason, refusal);
    }

    [Fact]
    public void A_drawing_cannot_reference_itself()
    {
        Assert.Contains("itself", XrefPathPolicy.Refuse(@"D:\a\Plan.dwg", @"D:\A\plan.DWG"));
        Assert.Null(XrefPathPolicy.Refuse(@"D:\a\Plan.dwg", @"D:\a\Other.dwg"));
    }

    [Fact]
    public void Unc_paths_are_recognised_so_the_caller_can_warn_about_the_synchronous_resolve()
    {
        Assert.True(XrefPathPolicy.IsUnc(@"\\server\share\x.dwg"));
        Assert.False(XrefPathPolicy.IsUnc(@"D:\x.dwg"));
    }
}
