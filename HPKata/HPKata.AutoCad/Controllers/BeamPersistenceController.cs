using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using HPKata.Core.Beams.Models;
using HPKata.Core.Beams.Persistence;
using HPKata.UI.ViewModels;
using HPKata.UI.Views;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Controllers
{
    /// <summary>
    /// Controller orchestrating beam rebar persistence to DWG Extension Dictionaries and 2D UI preview
    /// (Save_thep_dam, Save_thep_dam_all, Add_thep_dam, show_thep_dam).
    /// </summary>
    public static class BeamPersistenceController
    {
        public const string BeamDataDictName = "KATA_BEAM_DATA";

        public static List<BeamRebarLayout> LoadAllBeamsFromDwg(Database db)
        {
            var layouts = new List<BeamRebarLayout>();
            if (db == null) return layouts;

            using var tr = db.TransactionManager.StartTransaction();
            var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
            if (!nod.Contains(BeamDataDictName)) return layouts;

            var beamDict = (DBDictionary)tr.GetObject(nod.GetAt(BeamDataDictName), OpenMode.ForRead);
            foreach (DBDictionaryEntry entry in beamDict)
            {
                var xrec = (Xrecord)tr.GetObject(entry.Value, OpenMode.ForRead);
                if (xrec.Data == null) continue;

                var typedVals = xrec.Data.AsArray();
                if (typedVals.Length > 0 && typedVals[0].Value is string json)
                {
                    var layout = BeamXRecordSerializer.DeserializeFromJson(json);
                    if (layout != null) layouts.Add(layout);
                }
            }
            tr.Commit();
            return layouts;
        }

        public static void SaveBeamRebarToDwg(BeamRebarLayout? layout = null)
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            layout ??= ResolveLayoutForSave(ed);
            if (layout == null) return;

            string json = BeamXRecordSerializer.SerializeToJson(layout);
            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var nod = (DBDictionary)tr.GetObject(doc.Database.NamedObjectsDictionaryId, OpenMode.ForWrite);
                DBDictionary beamDict = GetOrCreateBeamDictionary(nod, tr);

                Xrecord xrec = new Xrecord();
                xrec.Data = new ResultBuffer(new TypedValue((int)DxfCode.Text, json));
                beamDict.SetAt(layout.BeamName, xrec);
                tr.AddNewlyCreatedDBObject(xrec, true);

                tr.Commit();
                BeamTakeoffRegistry.Register(layout);
                ed.WriteMessage($"\n[HPKata] Saved beam '{layout.BeamName}' model into DWG dictionary (JSON payload {json.Length} chars).\n");
            }
        }

        public static void SaveAllBeamsToDwg()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var layouts = BeamTakeoffRegistry.GetAll().ToList();
            if (layouts.Count == 0)
            {
                ed.WriteMessage("\n[HPKata] Save_thep_dam_all: No active beam models cached in registry to save.\n");
                return;
            }

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                var nod = (DBDictionary)tr.GetObject(doc.Database.NamedObjectsDictionaryId, OpenMode.ForWrite);
                DBDictionary beamDict = GetOrCreateBeamDictionary(nod, tr);

                foreach (var layout in layouts)
                {
                    string json = BeamXRecordSerializer.SerializeToJson(layout);
                    Xrecord xrec = new Xrecord();
                    xrec.Data = new ResultBuffer(new TypedValue((int)DxfCode.Text, json));
                    beamDict.SetAt(layout.BeamName, xrec);
                    tr.AddNewlyCreatedDBObject(xrec, true);
                }

                tr.Commit();
                ed.WriteMessage($"\n[HPKata] Save_thep_dam_all: Serialized {layouts.Count} beam models into DWG extension dictionary '{BeamDataDictName}'.\n");
            }
        }

        public static void AddRebarLayerInteractive()
        {
            Document? doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            var pStr = ed.GetString(new PromptStringOptions("\n[HPKata] Enter beam name to add extra rebar layer: ") { DefaultValue = "D1" });
            if (pStr.Status != PromptStatus.OK) return;
            string beamName = pStr.StringResult.Trim();

            if (!BeamTakeoffRegistry.TryGet(beamName, out var layout) || layout == null)
            {
                layout = new BeamRebarLayout { BeamName = beamName, TotalLengthMm = 6000.0 };
            }

            var pPos = ed.GetKeywords(CreatePositionOptions());
            bool isTop = pPos.Status != PromptStatus.OK || pPos.StringResult == "Top";

            var layer = new ExtraBarLayerModel { LayerIndex = 1, BarCount = 2, DiameterMm = 20, StartStationXMm = 0.0, EndStationXMm = 2000.0 };
            if (isTop)
            {
                if (!layout.SupportTopExtraBars.TryGetValue(0, out var list)) { list = new(); layout.SupportTopExtraBars[0] = list; }
                list.Add(layer);
            }
            else
            {
                if (!layout.MidspanBottomExtraBars.TryGetValue(0, out var list)) { list = new(); layout.MidspanBottomExtraBars[0] = list; }
                list.Add(layer);
            }

            SaveBeamRebarToDwg(layout);
            ed.WriteMessage($"\n[HPKata] Add_thep_dam: Added 2Ø20 {(isTop ? "Top" : "Bottom")} extra layer to beam '{beamName}'.\n");
        }

        public static void PreviewBeamRebar2D()
        {
            var vm = new BeamDetailingViewModel();
            var window = new BeamDetailingWindow(vm);
            AcadApp.ShowModalWindow(window);
        }

        private static BeamRebarLayout ResolveLayoutForSave(Editor ed)
        {
            var first = BeamTakeoffRegistry.GetAll().FirstOrDefault();
            if (first != null) return first;

            var pStr = ed.GetString(new PromptStringOptions("\n[HPKata] Enter beam name to save: ") { DefaultValue = "D1" });
            string name = pStr.Status == PromptStatus.OK ? pStr.StringResult.Trim() : "D1";
            return new BeamRebarLayout { BeamName = name, TotalLengthMm = 6000.0 };
        }

        private static DBDictionary GetOrCreateBeamDictionary(DBDictionary nod, Transaction tr)
        {
            if (nod.Contains(BeamDataDictName))
            {
                return (DBDictionary)tr.GetObject(nod.GetAt(BeamDataDictName), OpenMode.ForWrite);
            }

            var beamDict = new DBDictionary();
            nod.SetAt(BeamDataDictName, beamDict);
            tr.AddNewlyCreatedDBObject(beamDict, true);
            return beamDict;
        }

        private static PromptKeywordOptions CreatePositionOptions()
        {
            var opt = new PromptKeywordOptions("\n[HPKata] Select layer position [Top/Bottom]: ");
            opt.Keywords.Add("Top");
            opt.Keywords.Add("Bottom");
            opt.Keywords.Default = "Top";
            return opt;
        }
    }
}
