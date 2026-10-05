using System.Collections.Generic;
using System.Linq;

namespace HPRebar.Core.KataRebar.Calculators;

/// <summary>
/// A host's numbering of one partition: the numbers in use and a renumber that may be refused (Revit refuses a
/// number held by bars it does not call identical, or an element it cannot edit).
/// </summary>
public interface IKataNumberingTarget
{
    bool IsUsed(int number);

    int Highest();

    /// <summary>Gives every bar numbered <paramref name="from"/> the number <paramref name="to"/>; false when refused.</summary>
    bool TryChange(int from, int to);
}

/// <summary>
/// One number of a partition as the host has it: the Kata numbers its Kata bars carry, and how many of its bars
/// Kata Rebar did not draw (a renumber moves those too).
/// </summary>
public sealed record KataNumberGroup(int Revit, IReadOnlyList<int> Kata, int Foreign);

public sealed record KataNumberSwapResult(int Changed, IReadOnlyList<string> Warnings);

/// <summary>
/// Turns a partition's numbers into the Kata numbers of their bars. Every number to change is first moved above
/// the highest in use, so two numbers can trade places; then every Kata number is tried before any refusal is
/// settled, so a number given back on refusal cannot block a later Kata number. A number shared with bars Kata
/// Rebar did not draw is left alone; a refused one goes back to the host's number, or the lowest free one.
/// </summary>
public static class KataRebarNumberSwap
{
    private const int TemporaryGap = 1000;

    public static KataNumberSwapResult Apply(string beam, IReadOnlyList<KataNumberGroup> groups, IKataNumberingTarget target)
    {
        var warnings = new List<string>();
        var wanted = new List<(int Revit, int Kata)>();
        foreach (var group in groups.Where(g => g.Kata.Count > 0))
        {
            int kata = group.Kata.Min();
            if (group.Foreign > 0)
            {
                warnings.Add($"Dầm {beam}: số Revit {group.Revit} có cả {group.Foreign} thanh không do Kata Rebar vẽ — giữ nguyên, không ghi số Kata {kata}.");
                continue;
            }

            if (group.Kata.Count > 1)
                warnings.Add($"Dầm {beam}: Revit coi các thanh số Kata {string.Join(", ", group.Kata.OrderBy(n => n))} là giống nhau — ghi số {kata}.");
            if (group.Revit != kata) wanted.Add((group.Revit, kata));
        }

        var moved = MoveAside(beam, wanted, target, warnings);
        var refused = moved.Where(m => !target.TryChange(m.Temporary, m.Kata)).ToList();
        foreach (var (temporary, kata, revit) in refused)
            warnings.Add(GiveBack(beam, temporary, kata, revit, target));

        return new KataNumberSwapResult(moved.Count - refused.Count, warnings);
    }

    private static List<(int Temporary, int Kata, int Revit)> MoveAside(
        string beam, IReadOnlyList<(int Revit, int Kata)> wanted, IKataNumberingTarget target, List<string> warnings)
    {
        var moved = new List<(int Temporary, int Kata, int Revit)>();
        int temporary = target.Highest() + TemporaryGap;
        foreach (var (revit, kata) in wanted)
        {
            if (target.TryChange(revit, temporary))
                moved.Add((temporary++, kata, revit));
            else
                warnings.Add($"Dầm {beam}: Revit không cho đổi số {revit} — các thanh đó giữ số {revit}, không phải số Kata {kata}.");
        }

        return moved;
    }

    private static string GiveBack(string beam, int temporary, int kata, int revit, IKataNumberingTarget target)
    {
        int keep = target.IsUsed(revit) ? LowestFree(target) : revit;
        if (!target.TryChange(temporary, keep)) keep = temporary;
        return $"Dầm {beam}: Revit không cho đặt Rebar Number {kata} (số Kata) — các thanh đó giữ số {keep}.";
    }

    private static int LowestFree(IKataNumberingTarget target)
    {
        int n = 1;
        while (target.IsUsed(n)) n++;
        return n;
    }
}
