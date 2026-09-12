using System;
using Autodesk.Revit.DB;
using HPRebar.Core.FoundationRebar.Calculators;
using HPRebar.Core.FoundationRebar.Models;
using HPRebar.FoundationRebar.Model;
using Serilog;

namespace HPRebar.FoundationRebar.Service;

/// <summary>
///     Owner of the atomic TransactionGroup("Foundation Rebar"): calculates the mat, writes it, and either
///     assimilates the whole thing into one undo step or rolls it back. Opens a transaction, so it must be
///     called from the external event handler on Revit's API thread and never straight from the window.
/// </summary>
public sealed class FoundationRebarOrchestrator
{
    public FoundationMeshResult Run(FoundationSession session)
    {
        if (session is null) throw new ArgumentNullException(nameof(session));

        var document = session.Document;

        using var group = new TransactionGroup(document, "Foundation Rebar");
        group.Start();

        try
        {
            var mesh = FoundationMeshCalculator.Calculate(session.Snapshot, session.Spec);

            Log.Information(
                "Foundation Rebar generated mesh: {TotalBars} bars total ({Bx} Bx, {By} By, {Tx} Tx, {Ty} Ty), {Weight:F1} kg.",
                mesh.Statistics.TotalBarCount,
                mesh.Statistics.BottomBarCountX,
                mesh.Statistics.BottomBarCountY,
                mesh.Statistics.TopBarCountX,
                mesh.Statistics.TopBarCountY,
                mesh.Statistics.EstimatedWeightKg);

            using (var transaction = new Transaction(document, "Create Foundation Reinforcement"))
            {
                transaction.Start();
                RebarFailureHandling.Apply(transaction);

                FoundationRebarCreationService.CreateRebars(document, session.Floor, mesh, session);

                transaction.Commit();
            }

            group.Assimilate();

            return mesh;
        }
        catch (Exception exception)
        {
            Log.Error(exception, "Foundation Rebar orchestrator failed; rolling back the transaction group");
            group.RollBack();
            throw;
        }
    }
}
