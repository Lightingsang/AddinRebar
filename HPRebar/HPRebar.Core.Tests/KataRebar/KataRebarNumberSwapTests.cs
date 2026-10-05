using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.KataRebar.Calculators;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// The Rebar Number swap against a numbering that behaves like Revit's: a number moves only to a free number, and a
/// locked number (an element the user cannot edit) never moves.
/// </summary>
public sealed class KataRebarNumberSwapTests
{
    [Fact]
    public void Two_numbers_trade_places_through_a_temporary_number()
    {
        var revit = new FakeNumbering(1, 2);

        var result = KataRebarNumberSwap.Apply("B01", new[] { Group(1, 2), Group(2, 1) }, revit);

        Assert.Equal(2, result.Changed);
        Assert.Empty(result.Warnings);
        Assert.Equal(new[] { 1, 2 }, revit.Numbers);
        Assert.Equal("was 2", revit.Owner(1));
    }

    [Fact]
    public void A_number_shared_with_bars_kata_did_not_draw_is_left_alone()
    {
        var revit = new FakeNumbering(4);

        var result = KataRebarNumberSwap.Apply("B01", new[] { new KataNumberGroup(4, new[] { 7 }, Foreign: 1) }, revit);

        Assert.Equal(0, result.Changed);
        Assert.Equal(new[] { 4 }, revit.Numbers);
        Assert.Contains("không do Kata Rebar vẽ", Assert.Single(result.Warnings));
    }

    [Fact]
    public void A_number_given_back_on_refusal_does_not_block_a_later_kata_number()
    {
        // 9 belongs to another tool's bars, so 3 cannot become 9; 1 still becomes 3, and 3's bars take the free 1.
        var revit = new FakeNumbering(1, 3, 9);

        var result = KataRebarNumberSwap.Apply("B01", new[] { Group(3, 9), Group(1, 3) }, revit);

        Assert.Equal(1, result.Changed);
        Assert.Equal("was 1", revit.Owner(3));
        Assert.Equal("was 3", revit.Owner(1));
        Assert.Contains("giữ số 1", Assert.Single(result.Warnings));
    }

    [Fact]
    public void Two_numbers_wanting_one_kata_number_keep_the_second_on_its_revit_number()
    {
        var revit = new FakeNumbering(5, 6);

        var result = KataRebarNumberSwap.Apply("B01", new[] { Group(5, 4), Group(6, 4) }, revit);

        Assert.Equal(1, result.Changed);
        Assert.Equal("was 5", revit.Owner(4));
        Assert.Equal("was 6", revit.Owner(6));
        Assert.Contains("giữ số 6", Assert.Single(result.Warnings));
    }

    [Fact]
    public void A_number_revit_will_not_move_is_reported_and_the_rest_still_change()
    {
        var revit = new FakeNumbering(1, 2) { Locked = { 1 } };

        var result = KataRebarNumberSwap.Apply("B01", new[] { Group(1, 5), Group(2, 6) }, revit);

        Assert.Equal(1, result.Changed);
        Assert.Equal(new[] { 1, 6 }, revit.Numbers);
        Assert.Contains("không cho đổi số 1", Assert.Single(result.Warnings));
    }

    [Fact]
    public void Bars_revit_calls_identical_take_the_lowest_of_their_kata_numbers()
    {
        var revit = new FakeNumbering(3);

        var result = KataRebarNumberSwap.Apply("B01", new[] { new KataNumberGroup(3, new[] { 9, 8 }, 0) }, revit);

        Assert.Equal(new[] { 8 }, revit.Numbers);
        Assert.Contains("8, 9", Assert.Single(result.Warnings));
    }

    private static KataNumberGroup Group(int revit, int kata) => new(revit, new[] { kata }, 0);

    private sealed class FakeNumbering : IKataNumberingTarget
    {
        private readonly Dictionary<int, string> _owners;

        public FakeNumbering(params int[] numbers) => _owners = numbers.ToDictionary(n => n, n => $"was {n}");

        public HashSet<int> Locked { get; } = new();

        public int[] Numbers => _owners.Keys.OrderBy(n => n).ToArray();

        public string Owner(int number) => _owners[number];

        public bool IsUsed(int number) => _owners.ContainsKey(number);

        public int Highest() => _owners.Keys.DefaultIfEmpty(0).Max();

        public bool TryChange(int from, int to)
        {
            if (Locked.Contains(from) || !_owners.TryGetValue(from, out var owner) || _owners.ContainsKey(to)) return false;
            _owners.Remove(from);
            _owners[to] = owner;
            return true;
        }
    }
}
