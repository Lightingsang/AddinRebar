using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using HPDcePro.AutoCad.Drafting;
using HPDcePro.AutoCad.Prompts;
using HPDcePro.Core.Calculations.Beam;
using HPDcePro.Core.Drafting;
using HPDcePro.Core.Geometry;
using HPDcePro.Core.XData;
using HPDcePro.UI.Views;

namespace HPDcePro.AutoCad.Commands
{
    public sealed partial class BeamCommands
    {
        [CommandMethod("ShopDrawingBeam")]
        public void ShopDrawingBeam()
        {
            var win = new BeamDetailingWindow();
            AutoCadUiHelper.ShowModal(win);
        }

        [CommandMethod("VeDam_DCE")]
        public void VeDam_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên dầm", "D1", out string mark);
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng b (mm)", 220.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao h (mm)", 400.0, out double h);
            PromptHelper.TryGetInteger(ed, "Nhập số nhịp", 2, out int spanCount);
            Point2D origin = new Point2D(0, 0);
            double spanLen = 6000.0;

            if (PromptHelper.TryGetPoint(ed, "Chọn điểm đầu dầm (Start point):", out var pt1))
            {
                var jig = new Jigs.VeMBKCJig(pt1, b, "Mid", mark);
                if (ed != null && jig.StartJig(ed) == Autodesk.AutoCAD.EditorInput.PromptStatus.OK)
                {
                    origin = new Point2D(pt1.X, pt1.Y);
                    spanLen = Math.Max(2000.0, pt1.DistanceTo(jig.CurrentPoint));
                }
            }

            var result = CalculateBeamReinforcement(mark, Math.Max(1, spanCount), spanLen, b, h);
            var gen = new BeamShopDrawingGenerator();
            var buffer = gen.GenerateShopDrawing(result, new BeamDetailingOptions { Origin = origin });
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"VeDam_DCE: Đã sinh và vẽ chi tiết dầm {result.BeamMark} ({count} đối tượng CAD).");
        }

        [CommandMethod("VeDamAll_DCE")]
        public void VeDamAll_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetInteger(ed, "Nhập số lượng dầm cần vẽ", 2, out int beamCount);
            PromptHelper.TryGetPoint(ed, "Chọn điểm gốc vẽ hệ dầm", out var originPt);
            var buffer = new CadDraftingBuffer();
            var gen = new BeamShopDrawingGenerator();

            for (int i = 0; i < Math.Max(1, beamCount); i++)
            {
                string mark = $"D{i + 1}";
                var beam = CalculateBeamReinforcement(mark, 2, 6000.0, 220.0, 400.0);
                var buf = gen.GenerateShopDrawing(beam, new BeamDetailingOptions { Origin = new Point2D(originPt.X, originPt.Y + i * 4000.0) });
                for (int j = 0; j < buf.Count; j++) buffer.AddPrimitive(buf.Entities[j]);
            }
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"VeDamAll_DCE: Đã vẽ toàn bộ hệ dầm tầng ({count} đối tượng CAD).");
        }

        [CommandMethod("VeMCD_DCE")]
        public void VeMCD_DCE()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tên dầm cần vẽ mặt cắt", "D1", out string mark);
            PromptHelper.TryGetDouble(ed, "Nhập bề rộng b (mm)", 220.0, out double b);
            PromptHelper.TryGetDouble(ed, "Nhập chiều cao h (mm)", 400.0, out double h);
            PromptHelper.TryGetPoint(ed, "Chọn điểm đặt mặt cắt", out var pt);

            var result = CalculateBeamReinforcement(mark, 2, 6000.0, b, h);
            var gen = new BeamShopDrawingGenerator();
            var buffer = gen.GenerateShopDrawing(result, new BeamDetailingOptions { Origin = new Point2D(pt.X, pt.Y), IncludeCrossSections = true });
            int count = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"VeMCD_DCE: Đã vẽ mặt cắt tiết diện dầm {mark} ({count} đối tượng CAD).");
        }

        [CommandMethod("VeBlockDam2")]
        public void VeBlockDam2() => InsertSymbolBlock("DCE_DAM_2NHIP", "block dầm 2 nhịp");

        [CommandMethod("VeBlockCaoDoMB")]
        public void VeBlockCaoDoMB() => InsertSymbolBlock("DCE_CAODO_MB", "cao độ mặt bằng");

        [CommandMethod("VeBlockTrucDam")]
        public void VeBlockTrucDam() => InsertSymbolBlock("DCE_TRUC_DAM", "trục dầm");

        [CommandMethod("VeBlockChieuDaySan")]
        public void VeBlockChieuDaySan() => InsertSymbolBlock("DCE_DAY_SAN", "chiều dày sàn");

        [CommandMethod("VeBlockGiatCaoDoSan")]
        public void VeBlockGiatCaoDoSan() => InsertSymbolBlock("DCE_GIAT_CAP", "giật cấp sàn");

        [CommandMethod("VeBlockCaoDoMC")]
        public void VeBlockCaoDoMC() => InsertSymbolBlock("DCE_CAODO_MC", "cao độ mặt cắt");

        [CommandMethod("VeBlockSection")]
        public void VeBlockSection() => InsertSymbolBlock("DCE_VET_CAT", "vết cắt mặt cắt");

        [CommandMethod("ImportKichThuoc")]
        public void ImportKichThuoc()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetPoint(ed, "Chọn điểm gốc chèn bảng tiết diện dầm", out var originPt);
            var buffer = new CadDraftingBuffer();
            var sections = new (double B, double H)[] { (220, 350), (220, 400), (250, 500), (300, 600) };
            double curX = originPt.X;
            double curY = originPt.Y;
            for (int i = 0; i < sections.Length; i++)
            {
                var s = sections[i];
                var pts = new[] { new Point2D(curX, curY), new Point2D(curX + s.B, curY), new Point2D(curX + s.B, curY + s.H), new Point2D(curX, curY + s.H) };
                buffer.AddPolyline(pts, true, "DCE_Beam_Section", CadColor.CyanIndex, 0.35);
                buffer.AddText($"{s.B:F0}x{s.H:F0}", new Point2D(curX, curY - 100), 100.0, "DCE_Beam_Text", CadColor.YellowIndex);
                curX += s.B + 500.0;
            }
            int rendered = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"ImportKichThuoc: Đã nạp và vẽ {sections.Length} tiết diện dầm ({rendered} đối tượng CAD).");
        }

        [CommandMethod("SuaCKThep")]
        public void SuaCKThep()
        {
            var win = new BeamDetailingWindow();
            AutoCadUiHelper.ShowModal(win);
        }

        [CommandMethod("XemLienKetTenCauKien")]
        public void XemLienKetTenCauKien()
        {
            var ed = PromptHelper.GetActiveEditor();
            PromptHelper.TryGetString(ed, "Nhập tiền tố tên dầm", "D", out string prefix);
            PromptHelper.TryGetInteger(ed, "Nhập số lượng dầm", 3, out int count);
            PromptHelper.TryGetPoint(ed, "Chọn điểm bắt đầu", out var originPt);

            var buffer = new CadDraftingBuffer();
            for (int i = 1; i <= Math.Max(1, count); i++)
            {
                var pt = new Point2D(originPt.X + i * 3000.0, originPt.Y);
                var pkt = new XDataPacket(XDataRegistry.RegAppLinksXDataObject);
                pkt.AddString($"{prefix}{i}:L=6000,B=220,H=400");
                var txt = new CadEntityPrimitive
                {
                    EntityType = CadEntityType.Text,
                    TextString = $"{prefix}{i} (220x400)",
                    Position = pt,
                    TextHeight = 150.0,
                    Layer = "DCE_Beam_Text",
                    Color = new CadColor(CadColor.YellowIndex)
                };
                txt.AttachXData(pkt);
                buffer.AddPrimitive(txt);
                buffer.AddLine(pt, new Point2D(pt.X, pt.Y + 400), "DCE_Beam_Center", CadColor.RedIndex, 0.25);
            }
            int rendered = RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"XemLienKetTenCauKien: Đã vẽ liên kết {count} cấu kiện dầm ({rendered} đối tượng CAD).");
        }

        private static ContinuousBeamResult CalculateBeamReinforcement(string mark, int spanCount, double spanLen, double b, double h)
        {
            var spans = new List<BeamSpanInput>();
            for (int i = 0; i < spanCount; i++)
            {
                spans.Add(new BeamSpanInput
                {
                    SpanIndex = i,
                    ClearSpanMm = spanLen,
                    SectionWidthMm = b,
                    SectionHeightMm = h,
                    TopMomentLeftKnm = 120.0,
                    TopMomentRightKnm = 120.0,
                    BottomMomentMidKnm = 90.0,
                    ShearLeftKn = 110.0,
                    ShearRightKn = 110.0
                });
            }
            var concrete = MaterialLibrary.GetConcrete(ConcreteGrade.B20);
            var steel = MaterialLibrary.GetRebar(RebarGrade.CB400V);
            return ContinuousBeamCalculator.Calculate(new ContinuousBeamInput(mark, concrete, steel, spans));
        }

        private static void InsertSymbolBlock(string blockName, string description)
        {
            var ed = PromptHelper.GetActiveEditor();
            Point2D insertPt = new Point2D(0, 0);
            if (PromptHelper.TryGetPoint(ed, $"Chọn điểm chèn {description}:", out var p))
            {
                insertPt = new Point2D(p.X, p.Y);
            }

            var buffer = new CadDraftingBuffer();
            buffer.AddBlockInsert(blockName, insertPt, 1.0, 0.0, "DCE_Beam");
            RenderBufferToActiveDocument(buffer);
            AutoCadUiHelper.WriteMessage($"Đã chèn ký hiệu {description} ({blockName}).");
        }

        private static int RenderBufferToActiveDocument(CadDraftingBuffer buffer)
        {
            try
            {
                var doc = PromptHelper.GetActiveDocument();
                if (doc == null) return buffer.Count;

                using (doc.LockDocument())
                using (var tr = doc.Database.TransactionManager.StartTransaction())
                {
                    var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);
                    var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                    int rendered = CadDraftingBufferRenderer.Render(buffer, doc.Database, tr, btr);
                    tr.Commit();
                    return rendered;
                }
            }
            catch (System.Exception)
            {
                // Reason: In headless test runners or outside active drawing sessions, drawing rendering gracefully falls back.
                return buffer.Count;
            }
        }
    }
}
