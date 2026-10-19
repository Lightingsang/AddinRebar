namespace HPBim.Client.Tests.Pipeline;

using System;
using System.Linq;
using HPBim.Client.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

public sealed class BimPlanRunnerScriptTemplateTier5AdversarialTests
{
    [Fact]
    public void GetScriptCode_ParsesAsValidCSharpScript_WithZeroSyntaxErrors()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();
        Assert.False(string.IsNullOrWhiteSpace(script));

        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, kind: SourceCodeKind.Script);
        var tree = CSharpSyntaxTree.ParseText(script, parseOptions);
        var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        Assert.Empty(errors);
    }

    [Fact]
    public void Preamble_ContainsMandatoryCoordinateAndPreflightHelpers()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();

        Assert.Contains("double Ft(double mm) => mm / 304.8;", script);
        Assert.Contains("double Mm(double ft) => ft * 304.8;", script);
        Assert.Contains("XYZ ToXyz(double xMm, double yMm, double zMm = 0.0) => new XYZ(Ft(xMm), Ft(yMm), Ft(zMm));", script);
        Assert.Contains("[PRE_FLIGHT_FAILED]", script);
        Assert.Contains("Document.GetDocumentVersion(doc)", script);
        Assert.Contains("targetDoc.Str(\"versionGuid\"", script);
    }

    [Fact]
    public void ScriptTemplate_ContainsAllMandatoryCategories()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();

        string[] expectedCategories =
        {
            "category == \"Level\"",
            "category == \"Grid\"",
            "category == \"Column\"",
            "category == \"Beam\"",
            "category == \"GradeBeam\"",
            "category == \"Wall\"",
            "category == \"Slab\"",
            "category == \"Opening\"",
            "category == \"Room\"",
            "category == \"MepFixture\"",
            "category == \"PipeRun\"",
            "category == \"Rebar\""
        };

        foreach (var cat in expectedCategories)
        {
            Assert.Contains(cat, script);
        }
    }

    [Fact]
    public void ScriptTemplate_ContainsUnsupportedCategoryDefensiveBranch()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();

        Assert.Contains("ERR_UNSUPPORTED_CATEGORY", script);
        Assert.Contains("is unsupported by BimPlanRunner", script);
    }

    [Fact]
    public void ScriptTemplate_ContainsExtensibleStorageReceiptSchemaDefinition()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();

        Assert.Contains("D4E5F6A7-8B9C-0D1E-2F3A-4B5C6D7E8F90", script);
        Assert.Contains("HPBimCoordinationReceipt", script);
        Assert.Contains("builder.AddSimpleField(\"OperationId\", typeof(Guid));", script);
        Assert.Contains("builder.AddSimpleField(\"PlanHash\", typeof(string));", script);
        Assert.Contains("builder.AddSimpleField(\"CommittedAtUtc\", typeof(string));", script);
        Assert.Contains("builder.AddSimpleField(\"ElementMapJson\", typeof(string));", script);
        Assert.Contains("DataStorage.Create(doc)", script);
    }

    [Fact]
    public void ScriptTemplate_Epilogue_ReturnsExpectedSummaryStructure()
    {
        string script = BimPlanRunnerScriptTemplate.GetScriptCode();

        Assert.Contains("planId = args.Str(\"planId\", string.Empty)", script);
        Assert.Contains("total = items.Count", script);
        Assert.Contains("created = records.Count(", script);
        Assert.Contains("updated = records.Count(", script);
        Assert.Contains("noOp = records.Count(", script);
        Assert.Contains("skipped = records.Count(", script);
        Assert.Contains("failed = records.Count(", script);
        Assert.Contains("records = records", script);
    }
}
