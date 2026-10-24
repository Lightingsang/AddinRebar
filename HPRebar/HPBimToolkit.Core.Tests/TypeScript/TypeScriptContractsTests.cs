using System.Reflection;
using System.Text;
using HPBimToolkit.Core.Contracts;
using Xunit;

namespace HPBimToolkit.Core.Tests.TypeScript;

/// <summary>
///     Keeps the frontend's types in step with the C# contracts: the checked-in contracts.generated.ts must equal what
///     <see cref="TypeScriptContractWriter" /> renders now. Run with HPBIM_UPDATE_CONTRACTS=1 to rewrite it.
/// </summary>
public sealed class TypeScriptContractsTests
{
    private const string UpdateVariable = "HPBIM_UPDATE_CONTRACTS";

    [Fact]
    public void GeneratedFile_MatchesTheContracts()
    {
        var expected = TypeScriptContractWriter.Write();
        var path = GeneratedFilePath();

        var actual = File.Exists(path) ? File.ReadAllText(path).ReplaceLineEndings("\n") : null;
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            File.WriteAllText(path, expected, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            // Fails once after a rewrite, so a run that left the variable set never hides drift.
            Assert.True(actual == expected, $"{Path.GetFileName(path)} was rewritten from the contracts: review and commit it.");
            return;
        }

        Assert.True(actual is not null, $"{path} is missing. Run `dotnet test` with {UpdateVariable}=1 to create it.");
        Assert.True(actual == expected,
            $"{Path.GetFileName(path)} is out of date with HPBimToolkit.Core/Contracts. Run `dotnet test` with {UpdateVariable}=1 and commit it.");
    }

    [Fact]
    public void Catalog_ListsEveryCommandName()
    {
        Assert.Equal(
            ConstantValues(typeof(CommandNames)).Order(),
            ProtocolCatalog.Commands.Select(command => command.Name).Order());
    }

    [Fact]
    public void Catalog_ListsEveryEventName()
    {
        Assert.Equal(
            ConstantValues(typeof(EventNames)).Order(),
            ProtocolCatalog.Events.Select(@event => @event.Name).Order());
    }

    [Fact]
    public void Write_RendersNullableOptionalAndUnionMembers()
    {
        var ts = TypeScriptContractWriter.Write();

        Assert.Contains("  viewId: number | null;\n", ts);
        Assert.Contains("  categoryId?: number | null;\n", ts);
        Assert.Contains("  ids: number[];\n", ts);
        Assert.Contains("  theme: Theme;\n", ts);
        Assert.Contains("  scope?: StatisticsScope;\n", ts);
        Assert.Contains("export type StatisticsScope = 'model' | 'activeView';\n", ts);
        Assert.Contains("  'app.hello': { request: undefined; response: HelloResult };\n", ts);
        Assert.Contains("  'model.stale': ModelStaleEvent;\n", ts);
    }

    [Fact]
    public void Write_RefusesTypesWithoutATypeScriptMapping()
    {
        Assert.Throws<NotSupportedException>(() => TypeScriptContractWriter.CollectOnly(typeof(UnmappedPayload)));
    }

    private sealed record UnmappedPayload(Guid Id, IReadOnlyDictionary<string, int> Counts);

    private static IEnumerable<string> ConstantValues(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!);

    /// <summary>The frontend sits beside this test project under HPRebar/.</summary>
    private static string GeneratedFilePath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var frontend = Path.Combine(directory.FullName, "HPBimToolkit.Frontend");
            if (Directory.Exists(frontend))
            {
                return Path.Combine(frontend, "src", "bridge", "contracts.generated.ts");
            }
        }

        throw new DirectoryNotFoundException($"HPBimToolkit.Frontend not found above {AppContext.BaseDirectory}.");
    }
}
