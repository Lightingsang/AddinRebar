using ETABSv1;
using HPFoundation.Core.Modeling;
using static HPFoundation.App.Etabs.EtabsCall;

namespace HPFoundation.App.Etabs;

/// <summary>Saves the model, runs only the footing's load case and reads the forces back.</summary>
internal static class AnalysisRunner
{
    public static AnalysisResult Run(cSapModel model, string modelPath, IReadOnlyList<string> pointNames)
    {
        Check(model.File.Save(modelPath), "File.Save");
        Check(model.Analyze.SetRunCaseFlag("", false, true), "Analyze.SetRunCaseFlag");
        Check(model.Analyze.SetRunCaseFlag(ModelBuilder.LoadPatternName, true, false), "Analyze.SetRunCaseFlag");
        Check(model.Analyze.RunAnalysis(), "Analyze.RunAnalysis");

        Check(model.Results.Setup.DeselectAllCasesAndCombosForOutput(), "Results.Setup.DeselectAllCasesAndCombosForOutput");
        Check(model.Results.Setup.SetComboSelectedForOutput(ModelBuilder.ComboName), "Results.Setup.SetComboSelectedForOutput");

        return new AnalysisResult(ReadStations(model), ReadTotalReaction(model, pointNames));
    }

    private static List<StationForce> ReadStations(cSapModel model)
    {
        var frameCount = 0;
        string[] frameNames = [];
        Check(model.FrameObj.GetNameList(ref frameCount, ref frameNames), "FrameObj.GetNameList");

        return frameNames
            .SelectMany(frame => ReadFrame(model, frame))
            .OrderBy(station => station.XM)
            .ToList();
    }

    private static List<StationForce> ReadFrame(cSapModel model, string frame)
    {
        var count = 0;
        string[] objects = [], elements = [], cases = [], stepTypes = [];
        double[] objectStations = [], elementStations = [], stepNumbers = [], axial = [], shear2 = [], shear3 = [], torsion = [], moment2 = [], moment3 = [];
        Check(
            model.Results.FrameForce(frame, eItemTypeElm.ObjectElm, ref count, ref objects, ref objectStations, ref elements, ref elementStations, ref cases, ref stepTypes, ref stepNumbers, ref axial, ref shear2, ref shear3, ref torsion, ref moment2, ref moment3),
            "Results.FrameForce");

        var frameStartXM = FrameStartX(model, frame);
        return Enumerable
            .Range(0, count)
            .Select(index => new StationForce(frameStartXM + objectStations[index], shear2[index], moment3[index]))
            .ToList();
    }

    private static double FrameStartX(cSapModel model, string frame)
    {
        string start = string.Empty, end = string.Empty;
        Check(model.FrameObj.GetPoints(frame, ref start, ref end), "FrameObj.GetPoints");
        double x = 0, y = 0, z = 0;
        Check(model.PointObj.GetCoordCartesian(start, ref x, ref y, ref z), "PointObj.GetCoordCartesian");
        return x;
    }

    private static double ReadTotalReaction(cSapModel model, IReadOnlyList<string> pointNames) =>
        pointNames.Sum(point => ReadVerticalReaction(model, point));

    private static double ReadVerticalReaction(cSapModel model, string point)
    {
        var count = 0;
        string[] objects = [], elements = [], cases = [], stepTypes = [];
        double[] stepNumbers = [], force1 = [], force2 = [], force3 = [], moment1 = [], moment2 = [], moment3 = [];
        Check(
            model.Results.JointReact(point, eItemTypeElm.ObjectElm, ref count, ref objects, ref elements, ref cases, ref stepTypes, ref stepNumbers, ref force1, ref force2, ref force3, ref moment1, ref moment2, ref moment3),
            "Results.JointReact");
        return count > 0 ? force3[0] : 0;
    }
}
