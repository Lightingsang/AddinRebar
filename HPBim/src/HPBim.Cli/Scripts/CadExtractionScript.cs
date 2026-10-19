namespace HPBim.Cli.Scripts;

/// <summary>
///     Encapsulates the AutoCAD Roslyn C# script for extracting pile coordinates and grids.
///     Target: AutoCAD 2026 main-thread execution via Named Pipe.
/// </summary>
public static class CadExtractionScript
{
    public static string BuildScript() => """
        var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
        var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

        string GetEffectiveName(BlockReference br)
        {
            if (br.IsDynamicBlock)
            {
                var btr = (BlockTableRecord)tr.GetObject(br.DynamicBlockTableRecord, OpenMode.ForRead);
                return btr.Name;
            }
            return br.Name;
        }

        var gridRaw = new List<(string Name, double X, double Y)>();
        var pileBlocks = new List<(string Handle, double X, double Y)>();
        var textLabels = new List<(string Name, double X, double Y)>();

        foreach (ObjectId id in ms)
        {
            var ent = tr.GetObject(id, OpenMode.ForRead);
            if (ent is BlockReference br)
            {
                double px = br.Position.X;
                double py = br.Position.Y;
                if (px < 290000 || px > 365000 || py < -55000 || py > -5000) continue;

                string bname = GetEffectiveName(br);
                if (bname.IndexOf("TDV", StringComparison.OrdinalIgnoreCase) >= 0 || br.Layer.Equals("DAU TRUC", StringComparison.OrdinalIgnoreCase))
                {
                    string gName = "";
                    foreach (ObjectId attId in br.AttributeCollection)
                    {
                        var att = (AttributeReference)tr.GetObject(attId, OpenMode.ForRead);
                        string val = att.TextString.Trim();
                        if (!string.IsNullOrEmpty(val))
                        {
                            gName = val;
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(gName))
                    {
                        gridRaw.Add((gName, px, py));
                    }
                }
                else if (bname.Equals("Rec-TT", StringComparison.OrdinalIgnoreCase))
                {
                    pileBlocks.Add((br.Handle.ToString(), px, py));
                }
            }
            else if (ent is DBText dt && dt.Layer.Equals("STT-COC-B", StringComparison.OrdinalIgnoreCase))
            {
                double tx = dt.Position.X;
                double ty = dt.Position.Y;
                if (tx >= 290000 && tx <= 365000 && ty >= -55000 && ty <= -5000)
                {
                    string val = dt.TextString.Trim();
                    if (val.StartsWith("P", StringComparison.OrdinalIgnoreCase))
                    {
                        textLabels.Add((val, tx, ty));
                    }
                }
            }
            else if (ent is MText mt && mt.Layer.Equals("STT-COC-B", StringComparison.OrdinalIgnoreCase))
            {
                double tx = mt.Location.X;
                double ty = mt.Location.Y;
                if (tx >= 290000 && tx <= 365000 && ty >= -55000 && ty <= -5000)
                {
                    string val = mt.Contents.Trim();
                    if (val.StartsWith("P", StringComparison.OrdinalIgnoreCase))
                    {
                        textLabels.Add((val, tx, ty));
                    }
                }
            }
        }

        var xGrids = gridRaw
            .Where(g => g.Name.Length > 0 && char.IsDigit(g.Name[0]))
            .GroupBy(g => g.Name)
            .Select(grp => new { name = grp.Key, x = Math.Round(grp.Average(v => v.X), 1) })
            .OrderBy(g => int.TryParse(g.name, out int n) ? n : 999)
            .ToList();

        var yGrids = gridRaw
            .Where(g => g.Name.Length > 0 && char.IsLetter(g.Name[0]))
            .GroupBy(g => g.Name)
            .Select(grp => new { name = grp.Key, y = Math.Round(grp.Average(v => v.Y), 1) })
            .OrderBy(g => g.name)
            .ToList();

        double originX = xGrids.FirstOrDefault(g => g.name == "1")?.x ?? (xGrids.Count > 0 ? xGrids[0].x : 0.0);
        double originY = yGrids.FirstOrDefault(g => g.name == "A")?.y ?? (yGrids.Count > 0 ? yGrids[0].y : 0.0);

        var matchedPiles = new List<(int Stt, string Name, string Handle, double CadX, double CadY, double RevitX, double RevitY, double Dist)>();
        foreach (var pb in pileBlocks)
        {
            var nearest = textLabels
                .Select(t => new { t.Name, Dist = Math.Sqrt(Math.Pow(t.X - pb.X, 2) + Math.Pow(t.Y - pb.Y, 2)) })
                .Where(t => t.Dist < 1500)
                .OrderBy(t => t.Dist)
                .FirstOrDefault();

            string pName = nearest?.Name ?? "P?";
            double dist = nearest != null ? Math.Round(nearest.Dist, 1) : 0.0;
            int stt = 0;
            if (pName.StartsWith("P", StringComparison.OrdinalIgnoreCase) && int.TryParse(pName.Substring(1), out int s))
            {
                stt = s;
            }

            matchedPiles.Add((
                stt,
                pName,
                pb.Handle,
                Math.Round(pb.X, 1),
                Math.Round(pb.Y, 1),
                Math.Round(pb.X - originX, 1),
                Math.Round(pb.Y - originY, 1),
                dist
            ));
        }

        var sortedPiles = matchedPiles
            .Where(p => p.Stt >= 1 && p.Stt <= 196)
            .OrderBy(p => p.Stt)
            .Select(p => new
            {
                stt = p.Stt,
                name = p.Name,
                handle = p.Handle,
                cad_x = p.CadX,
                cad_y = p.CadY,
                revit_x = p.RevitX,
                revit_y = p.RevitY,
                dist_to_text = p.Dist
            })
            .ToList();

        return new
        {
            projectName = "Hội trường Tân Đông Hiệp",
            drawing = doc.Name,
            origin = new
            {
                description = "Giao điểm Trục 1 và Trục A",
                cad_x_mm = originX,
                cad_y_mm = originY
            },
            grids = new { x = xGrids, y = yGrids },
            totalPiles = sortedPiles.Count,
            pileSpecs = new
            {
                type = "Cọc vuông BTCT 300x300",
                section_mm = "300x300",
                width_mm = 300,
                length_m = 15.0,
                segments = "8m + 7m",
                top_elevation_m = -1.0,
                embedment_mm = 100
            },
            piles = sortedPiles
        };
        """;
}
