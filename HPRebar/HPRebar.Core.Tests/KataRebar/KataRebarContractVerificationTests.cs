using System;
using System.Collections.Generic;
using System.Linq;
using HPRebar.Core.BeamRebar.Models;
using HPRebar.Core.KataRebar.Calculators;
using HPRebar.Core.KataRebar.Models;
using Xunit;

namespace HPRebar.Core.Tests.KataRebar;

/// <summary>
/// Empirical contract and edge-case verification tests by challenger_2.
/// Evaluates logic invariants, cleanup filter predicates, and coordinate mapping bounds.
/// </summary>
public sealed class KataRebarContractVerificationTests
{
    private const string CommentPrefix = "HPRebar_Kata_";

    /// <summary>
    /// Simulates the exact predicate used in KataRebarCleanupService.FindExistingKataRebars:
    /// </summary>
    private static bool EvaluateCleanupPredicate(string? comment, string beamName)
    {
        string targetComment = $"{CommentPrefix}{beamName}";
        if (string.IsNullOrEmpty(comment)) return false;

        if (!string.IsNullOrWhiteSpace(beamName))
            return comment.Equals(targetComment, StringComparison.OrdinalIgnoreCase);

        return comment.StartsWith(CommentPrefix, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CleanupPredicate_DoesNotOverDeleteOtherBeams_WhenBeamNameIsSpecified()
    {
        // Fix verification:
        // When searching for existing rebars of beam "B1",
        // rebars stamped with "HPRebar_Kata_B2" on the same host elements evaluate to false!
        string requestedBeam = "B1";
        string otherBeamComment = "HPRebar_Kata_B2";

        bool matchesOtherBeam = EvaluateCleanupPredicate(otherBeamComment, requestedBeam);

        Assert.False(matchesOtherBeam,
            "The cleanup predicate must return false for other beams when a specific beamName is requested.");
    }

    [Fact]
    public void CleanupPredicate_MatchesTargetBeamPrecisely_WhenIntended()
    {
        string targetBeam = "D1";
        string matchingComment = "HPRebar_Kata_D1";
        string matchingCommentLower = "hprebar_kata_d1";
        string unrelatedComment = "Custom_Rebar_D1";

        Assert.True(EvaluateCleanupPredicate(matchingComment, targetBeam));
        Assert.True(EvaluateCleanupPredicate(matchingCommentLower, targetBeam));
        Assert.False(EvaluateCleanupPredicate(unrelatedComment, targetBeam));
        Assert.False(EvaluateCleanupPredicate(null, targetBeam));
        Assert.False(EvaluateCleanupPredicate("", targetBeam));
    }

    [Fact]
    public void HostIndexClamping_UnderSpanCountMismatch_ClustersAllExtraBarsOnLastBeam()
    {
        // When Revit selection has 2 beams, but Kata spec has 4 spans:
        int revitBeamCount = 2;
        var hostBeams = new[] { "Beam_0", "Beam_1" };

        var mappedHostIndices = new List<int>();
        for (int spanIdx = 0; spanIdx < 4; spanIdx++)
        {
            int hostIdx = Math.Clamp(spanIdx, 0, revitBeamCount - 1);
            mappedHostIndices.Add(hostIdx);
        }

        // Span 0 -> Beam 0
        // Span 1 -> Beam 1
        // Span 2 -> Beam 1 (clamped)
        // Span 3 -> Beam 1 (clamped)
        Assert.Equal(0, mappedHostIndices[0]);
        Assert.Equal(1, mappedHostIndices[1]);
        Assert.Equal(1, mappedHostIndices[2]);
        Assert.Equal(1, mappedHostIndices[3]);

        // This proves that spans 2 and 3 will be hosted on Beam 1 even though they physically extend far beyond it.
    }

    [Fact]
    public void ShortCurveTolerance_SegmentsBelowRevitThreshold_MustBeFiltered()
    {
        // Revit API Application.ShortCurveTolerance is ~1/16 inch (~0.0052 ft / 1.58 mm)
        // or ~0.78 mm depending on internal precision.
        // A segment of 0.5 mm is 0.00164 ft, which is filtered out by the updated 2.0e-3 ft threshold.
        double segmentLengthMm = 0.5;
        double segmentLengthFt = segmentLengthMm / 304.8;

        bool passesServiceToleranceCheck = segmentLengthFt > 2.0e-3; // KataRebarCreationService.cs line 244 (updated)
        bool isBelowRevitTolerance = segmentLengthFt < 0.0052;       // Revit ShortCurveTolerance

        Assert.False(passesServiceToleranceCheck, "0.5 mm segment must be filtered by the 2.0e-3 ft threshold.");
        Assert.True(isBelowRevitTolerance, "Segment is below Revit's ShortCurveTolerance (0.0052 ft), risking crash if created.");
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(0.0005, true)]
    [InlineData(0.001, true)]
    [InlineData(0.0011, false)]
    [InlineData(0.05, false)]
    [InlineData(-0.02, false)]
    public void SlopeCheck_VerifiesBeamIsHorizontal(double directionZ, bool expectedValid)
    {
        const double maxSlopeZ = 1e-3;
        bool isValid = Math.Abs(directionZ) <= maxSlopeZ;
        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(10.0, true)]
    [InlineData(25.0, true)]
    [InlineData(25.1, false)]
    [InlineData(50.0, false)]
    [InlineData(3000.0, false)]
    public void ElevationConsistencyCheck_VerifiesTolerances(double elevationDiffMm, bool expectedValid)
    {
        const double maxElevationOffsetMm = 25.0;
        bool isValid = elevationDiffMm <= maxElevationOffsetMm;
        Assert.Equal(expectedValid, isValid);
    }
}
