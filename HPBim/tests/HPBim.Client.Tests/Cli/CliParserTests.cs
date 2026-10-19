namespace HPBim.Client.Tests.Cli;

using System;
using HPBim.Cli.CommandLine;
using Xunit;

public sealed class CliParserTests
{
    [Fact]
    public void Parse_EmptyArgs_ReturnsEmptyResult()
    {
        var result = CliParser.Parse(Array.Empty<string>());
        Assert.Equal(string.Empty, result.Command);
        Assert.Empty(result.Options);
        Assert.Empty(result.Flags);
        Assert.Empty(result.UnmatchedArguments);
    }

    [Fact]
    public void Parse_NullArgs_ReturnsEmptyResult()
    {
        var result = CliParser.Parse(null);
        Assert.Equal(string.Empty, result.Command);
    }

    [Fact]
    public void Parse_ValidCommandAndInputOption_ParsesCorrectly()
    {
        string[] args = ["validate", "--input", "model.json"];
        var result = CliParser.Parse(args);

        Assert.Equal("validate", result.Command);
        Assert.Equal("model.json", result.GetOption("input"));
    }

    [Fact]
    public void Parse_DryRunFlag_IdentifiesFlagCorrectly()
    {
        string[] args = ["generate", "--input", "model.json", "--dry-run"];
        var result = CliParser.Parse(args);

        Assert.Equal("generate", result.Command);
        Assert.True(result.HasFlag("dry-run"));
        Assert.False(result.HasFlag("verbose"));
    }

    [Fact]
    public void Parse_MultipleOptionsAndFlags_ExtractsAllPairs()
    {
        string[] args = ["estimate", "--input", "m.json", "-o", "out.txt", "--verbose"];
        var result = CliParser.Parse(args);

        Assert.Equal("estimate", result.Command);
        Assert.Equal("m.json", result.GetOption("input"));
        Assert.Equal("out.txt", result.GetOption("o"));
        Assert.True(result.HasFlag("verbose"));
    }

    [Fact]
    public void Parse_PositionalAndUnmatchedArgs_Preserved()
    {
        string[] args = ["custom", "extra1", "--opt", "val", "extra2"];
        var result = CliParser.Parse(args);

        Assert.Equal("custom", result.Command);
        Assert.Equal("val", result.GetOption("opt"));
        Assert.Equal(2, result.UnmatchedArguments.Count);
        Assert.Contains("extra1", result.UnmatchedArguments);
        Assert.Contains("extra2", result.UnmatchedArguments);
    }

    [Fact]
    public void GetOption_FallbackUsed_WhenKeyNotFound()
    {
        var result = CliParser.Parse(["cmd"]);
        Assert.Equal("default.pipe", result.GetOption("pipe", "default.pipe"));
        Assert.Null(result.GetOption("nonexistent"));
    }
}
