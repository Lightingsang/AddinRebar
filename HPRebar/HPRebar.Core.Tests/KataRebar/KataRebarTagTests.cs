using System;
using HPRebar.Core.KataRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

public sealed class KataRebarTagTests
{
    private const string Host = "b1f9e2a4-1234-4a5b-9c3d-000000000abc-0009485d";

    [Fact]
    public void The_tag_names_its_host()
    {
        Assert.Equal("HPRebar_Kata:" + Host, KataRebarTag.ForHost(Host));
    }

    [Fact]
    public void Only_the_exact_tag_of_the_host_matches()
    {
        Assert.True(KataRebarTag.IsTagFor(KataRebarTag.ForHost(Host), Host));
        Assert.True(KataRebarTag.IsTagFor("  " + KataRebarTag.ForHost(Host) + " ", Host));
        Assert.False(KataRebarTag.IsTagFor(KataRebarTag.ForHost(Host + "x"), Host));
        Assert.False(KataRebarTag.IsTagFor("HPRebar_Kata_DT1", Host));
        Assert.False(KataRebarTag.IsTagFor("hprebar_kata:" + Host, Host));
        Assert.False(KataRebarTag.IsTagFor(null, Host));
        Assert.False(KataRebarTag.IsTagFor("", Host));
    }

    [Fact]
    public void A_host_without_an_id_cannot_be_tagged()
    {
        Assert.Throws<ArgumentException>(() => KataRebarTag.ForHost(" "));
        Assert.False(KataRebarTag.IsTagFor("HPRebar_Kata:", ""));
    }
}
