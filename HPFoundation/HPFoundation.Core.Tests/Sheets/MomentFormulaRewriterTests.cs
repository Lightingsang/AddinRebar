using HPFoundation.Core.Sheets;
using Xunit;

namespace HPFoundation.Core.Tests.Sheets;

public sealed class MomentFormulaRewriterTests
{
    private const string LongFormula = "=ROUND(SUM(D17:D20)+ABS(C17*F29+C18*G29-C19*H29-C20*I29)+SUM(E17:E20)*D24,2)";
    private const string ShortFormula = "=ROUND(SUM(F17:F20)+SUM(G17:G20)*D24,2)";

    [Fact]
    public void RewriteLongDirection_SwapsColumnMomentToMy_KeepsTheRest()
    {
        var rewritten = MomentFormulaRewriter.RewriteLongDirection(LongFormula);

        Assert.Equal("=ABS(ROUND(SUM(F17:F20)+ABS(C17*F29+C18*G29-C19*H29-C20*I29)+SUM(E17:E20)*D24,2))", rewritten);
    }

    [Fact]
    public void RewriteShortDirection_SwapsColumnMomentToMx_KeepsTheRest()
    {
        var rewritten = MomentFormulaRewriter.RewriteShortDirection(ShortFormula);

        Assert.Equal("=ABS(ROUND(SUM(D17:D20)+SUM(G17:G20)*D24,2))", rewritten);
    }

    [Fact]
    public void RewriteLongDirection_AlreadyRewritten_Throws()
    {
        var rewritten = MomentFormulaRewriter.RewriteLongDirection(LongFormula);

        Assert.Throws<ArgumentException>(() => MomentFormulaRewriter.RewriteLongDirection(rewritten));
    }

    [Fact]
    public void RewriteShortDirection_ForeignFormula_Throws()
    {
        Assert.Throws<ArgumentException>(() => MomentFormulaRewriter.RewriteShortDirection("=SUM(F17:F20)"));
    }
}
