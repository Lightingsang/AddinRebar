using System;
using System.Threading.Tasks;
using HPRebar.BeamRebar.Model;

namespace HPRebar.BeamRebar.ViewModel;

/// <summary>
/// Execution runner interface decoupling the ViewModel from direct Revit API document calls.
/// </summary>
public interface IBeamRebarRunner
{
    int PlannedCount(BeamRebarSpec spec);
    Task<int> RunAsync(BeamRebarSpec spec, IProgress<int> progress);
    BeamOrchestratorResult? LastResult { get; }
}
