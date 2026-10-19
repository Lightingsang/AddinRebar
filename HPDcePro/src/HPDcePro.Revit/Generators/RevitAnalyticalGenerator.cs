using System;
using System.Collections.Generic;
using System.Linq;
using HPDcePro.Core.Geometry;

namespace HPDcePro.Revit.Generators
{
    public sealed record AnalyticalMemberDef(
        string PhysicalElementId,
        string Category,
        Point3D StartMm,
        Point3D EndMm);

    public sealed record AnalyticalNodeDef(
        int NodeId,
        Point3D CoordinateMm,
        IReadOnlyList<string> ConnectedMemberIds);

    public sealed record AnalyticalModelPlan(
        IReadOnlyList<AnalyticalMemberDef> Members,
        IReadOnlyList<AnalyticalNodeDef> Nodes);

    public static class RevitAnalyticalGenerator
    {
        public static AnalyticalModelPlan GenerateAnalyticalModel(
            IEnumerable<AnalyticalMemberDef> members,
            double nodeToleranceMm = 25.0)
        {
            if (members == null)
            {
                return new AnalyticalModelPlan(Array.Empty<AnalyticalMemberDef>(), Array.Empty<AnalyticalNodeDef>());
            }

            var memberList = members.ToList();
            var nodes = new List<(Point3D Pt, List<string> Members)>();

            foreach (var m in memberList)
            {
                AddOrMergeNode(nodes, m.StartMm, m.PhysicalElementId, nodeToleranceMm);
                AddOrMergeNode(nodes, m.EndMm, m.PhysicalElementId, nodeToleranceMm);
            }

            var nodeDefs = new List<AnalyticalNodeDef>();
            for (int i = 0; i < nodes.Count; i++)
            {
                nodeDefs.Add(new AnalyticalNodeDef(i + 1, nodes[i].Pt, nodes[i].Members.Distinct().ToList()));
            }

            return new AnalyticalModelPlan(memberList, nodeDefs);
        }

        private static void AddOrMergeNode(
            List<(Point3D Pt, List<string> Members)> nodes,
            Point3D pt,
            string memberId,
            double tol)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].Pt.DistanceTo(pt) <= tol)
                {
                    nodes[i].Members.Add(memberId);
                    return;
                }
            }
            nodes.Add((pt, new List<string> { memberId }));
        }
    }

    public static class AnalyticalModelGenerator
    {
        public static AnalyticalModelPlan GenerateAnalyticalModel(
            IEnumerable<AnalyticalMemberDef> members,
            double nodeToleranceMm = 25.0) =>
            RevitAnalyticalGenerator.GenerateAnalyticalModel(members, nodeToleranceMm);
    }
}
