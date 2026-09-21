using System;
using System.Collections.Generic;

namespace HPRobot.McpBridge.Safety;

/// <summary>
///     Table of known Autodesk Robot Structural Analysis Professional (RobotOM)
///     member classifications for 3-tier safety analysis.
/// </summary>
public static class RobotTierTable
{
    private static readonly Dictionary<string, RobotTier> KnownMembers = new(StringComparer.OrdinalIgnoreCase)
    {
        // ==========================================
        // Tier R: Read-only queries
        // ==========================================
        // Application & Project
        ["Application.Visible"] = RobotTier.Read,
        ["Application.Version"] = RobotTier.Read,
        ["Application.ProgramVersion"] = RobotTier.Read,
        ["Project.IsActive"] = RobotTier.Read,
        ["Project.FileName"] = RobotTier.Read,
        ["Project.Type"] = RobotTier.Read,
        ["Project.Preferences"] = RobotTier.Read,
        ["Project.Structure"] = RobotTier.Read,
        ["Project.CalcEngine"] = RobotTier.Read,
        ["Project.AxisMngr"] = RobotTier.Read,

        // Nodes server & object
        ["Nodes.Count"] = RobotTier.Read,
        ["Nodes.Get"] = RobotTier.Read,
        ["Nodes.GetAll"] = RobotTier.Read,
        ["Nodes.Exist"] = RobotTier.Read,
        ["Nodes.FreeNumber"] = RobotTier.Read,
        ["Node.X"] = RobotTier.Read,
        ["Node.Y"] = RobotTier.Read,
        ["Node.Z"] = RobotTier.Read,
        ["Node.Number"] = RobotTier.Read,
        ["Node.HasLabel"] = RobotTier.Read,
        ["Node.GetLabel"] = RobotTier.Read,

        // Bars server & object
        ["Bars.Count"] = RobotTier.Read,
        ["Bars.Get"] = RobotTier.Read,
        ["Bars.GetAll"] = RobotTier.Read,
        ["Bars.Exist"] = RobotTier.Read,
        ["Bars.FreeNumber"] = RobotTier.Read,
        ["Bar.Number"] = RobotTier.Read,
        ["Bar.StartNode"] = RobotTier.Read,
        ["Bar.EndNode"] = RobotTier.Read,
        ["Bar.Length"] = RobotTier.Read,
        ["Bar.Gamma"] = RobotTier.Read,
        ["Bar.HasLabel"] = RobotTier.Read,
        ["Bar.GetLabel"] = RobotTier.Read,

        // Panels/Objects server & object
        ["Objects.Count"] = RobotTier.Read,
        ["Objects.Get"] = RobotTier.Read,
        ["Objects.GetAll"] = RobotTier.Read,
        ["Objects.Exist"] = RobotTier.Read,
        ["Objects.FreeNumber"] = RobotTier.Read,
        ["ObjObject.Number"] = RobotTier.Read,
        ["ObjObject.Main"] = RobotTier.Read,
        ["ObjObject.HasLabel"] = RobotTier.Read,
        ["ObjObject.GetLabel"] = RobotTier.Read,

        // Cases server & object
        ["Cases.Count"] = RobotTier.Read,
        ["Cases.Get"] = RobotTier.Read,
        ["Cases.GetAll"] = RobotTier.Read,
        ["Cases.Exist"] = RobotTier.Read,
        ["Cases.FreeNumber"] = RobotTier.Read,
        ["SimpleCase.Number"] = RobotTier.Read,
        ["SimpleCase.Name"] = RobotTier.Read,
        ["SimpleCase.Type"] = RobotTier.Read,
        ["SimpleCase.RecordsCount"] = RobotTier.Read,

        // Labels server & object
        ["Labels.Count"] = RobotTier.Read,
        ["Labels.Get"] = RobotTier.Read,
        ["Labels.Exist"] = RobotTier.Read,
        ["Label.Name"] = RobotTier.Read,
        ["Label.Type"] = RobotTier.Read,

        // Results server & values
        ["Results.Available"] = RobotTier.Read,
        ["Results.Nodes"] = RobotTier.Read,
        ["Results.Bars"] = RobotTier.Read,
        ["Reactions.Value"] = RobotTier.Read,
        ["Forces.Value"] = RobotTier.Read,
        ["Deflections.Value"] = RobotTier.Read,

        // Units manager
        ["UnitMngr.Get"] = RobotTier.Read,
        ["UnitMngr.GetName"] = RobotTier.Read,
        ["UnitMngr.GetCoeff"] = RobotTier.Read,
        ["UnitMngr.UseMetricAsDefault"] = RobotTier.Read,

        // ==========================================
        // Tier W: Write & Modify members
        // ==========================================
        ["Nodes.Create"] = RobotTier.Write,
        ["Nodes.FindXYZ"] = RobotTier.Write,
        ["Bars.Create"] = RobotTier.Write,
        ["Objects.Create"] = RobotTier.Write,
        ["Objects.CreateContour"] = RobotTier.Write,
        ["Cases.Create"] = RobotTier.Write,
        ["Cases.CreateSimple"] = RobotTier.Write,
        ["Cases.CreateCombination"] = RobotTier.Write,
        ["Labels.Create"] = RobotTier.Write,
        ["Labels.Store"] = RobotTier.Write,
        ["Node.SetLabel"] = RobotTier.Write,
        ["Bar.SetLabel"] = RobotTier.Write,
        ["Bar.SetPoints"] = RobotTier.Write,
        ["ObjObject.SetLabel"] = RobotTier.Write,
        ["ObjObject.Initialize"] = RobotTier.Write,
        ["SimpleCase.NewRecord"] = RobotTier.Write,
        ["SimpleCase.SetValue"] = RobotTier.Write,
        ["SimpleCase.SetFactor"] = RobotTier.Write,
        ["LoadRecord.SetValue"] = RobotTier.Write,
        ["UnitMngr.Set"] = RobotTier.Write,
        ["UnitMngr.Refresh"] = RobotTier.Write,
        ["Project.Save"] = RobotTier.Write,
        ["Project.SaveAs"] = RobotTier.Write,

        // ==========================================
        // Tier D: Delete & Heavy FEA solver members
        // ==========================================
        ["CalcEngine.Calculate"] = RobotTier.DeleteHeavy,
        ["CalcEngine.GenerateModel"] = RobotTier.DeleteHeavy,
        ["CalcEngine.AutoGenerateModel"] = RobotTier.DeleteHeavy,
        ["Structure.Clear"] = RobotTier.DeleteHeavy,
        ["Nodes.Delete"] = RobotTier.DeleteHeavy,
        ["Nodes.DeleteMany"] = RobotTier.DeleteHeavy,
        ["Nodes.DeleteAll"] = RobotTier.DeleteHeavy,
        ["Bars.Delete"] = RobotTier.DeleteHeavy,
        ["Bars.DeleteMany"] = RobotTier.DeleteHeavy,
        ["Bars.DeleteAll"] = RobotTier.DeleteHeavy,
        ["Objects.Delete"] = RobotTier.DeleteHeavy,
        ["Objects.DeleteMany"] = RobotTier.DeleteHeavy,
        ["Objects.DeleteAll"] = RobotTier.DeleteHeavy,
        ["Cases.Delete"] = RobotTier.DeleteHeavy,
        ["Cases.DeleteMany"] = RobotTier.DeleteHeavy,
        ["Cases.DeleteAll"] = RobotTier.DeleteHeavy,
        ["Labels.Delete"] = RobotTier.DeleteHeavy,
        ["Labels.DeleteMany"] = RobotTier.DeleteHeavy,
        ["Project.New"] = RobotTier.DeleteHeavy,
        ["Project.Close"] = RobotTier.DeleteHeavy,
        ["Application.Quit"] = RobotTier.DeleteHeavy
    };

    /// <summary>
    ///     Classifies a member name or expression into a RobotTier.
    /// </summary>
    public static RobotTier Classify(string memberName)
    {
        if (string.IsNullOrWhiteSpace(memberName))
            return RobotTier.Read;

        if (KnownMembers.TryGetValue(memberName, out var tier))
            return tier;

        // Strip receiver qualification
        var dot = memberName.LastIndexOf('.');
        var simpleName = dot >= 0 ? memberName[(dot + 1)..] : memberName;

        // 1. Delete / Heavy checks
        if (simpleName.Equals("Calculate", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("GenerateModel", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("AutoGenerateModel", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Delete", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("DeleteMany", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("DeleteAll", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Clear", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Quit", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Close", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("New", StringComparison.OrdinalIgnoreCase))
        {
            return RobotTier.DeleteHeavy;
        }

        // 2. Write / Mutation checks
        if (simpleName.Equals("Create", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("CreateSimple", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("CreateCombination", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("CreateContour", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("SetLabel", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("SetValue", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("SetFactor", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("SetPoints", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Store", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("NewRecord", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Save", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("SaveAs", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Initialize", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Add", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Modify", StringComparison.OrdinalIgnoreCase) ||
            simpleName.Equals("Insert", StringComparison.OrdinalIgnoreCase))
        {
            return RobotTier.Write;
        }

        return RobotTier.Read;
    }
}
