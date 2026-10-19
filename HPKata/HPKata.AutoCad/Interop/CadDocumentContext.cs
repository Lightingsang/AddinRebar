using System;
using System.Collections.Generic;
using System.IO;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using HPKata.AutoCad.Abstractions;
using HPKata.Core.Serialization;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Production implementation of ICadDocumentContext operating against active AutoCAD Document.
    /// </summary>
    public class CadDocumentContext : ICadDocumentContext
    {
        private readonly Document _doc;

        public CadDocumentContext(Document doc)
        {
            _doc = doc ?? throw new ArgumentNullException(nameof(doc));
        }

        public string DocumentPath => _doc.Database.Filename ?? string.Empty;
        public string DocumentName => Path.GetFileName(_doc.Name ?? "Drawing.dwg");

        public IReadOnlyList<CadEntityDto> GetAllRebarEntities()
        {
            var list = new List<CadEntityDto>();
            Database db = _doc.Database;

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                foreach (ObjectId id in ms)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is Entity ent && XDataService.HasKataXData(ent))
                    {
                        list.Add(MapEntityToDto(ent, tr));
                    }
                }
                tr.Commit();
            }
            return list;
        }

        public IReadOnlyList<CadEntityDto> GetSelectedRebarEntities()
        {
            var list = new List<CadEntityDto>();
            List<ObjectId> ids = CadSelectionService.PromptSelectMultiple(
                _doc.Editor,
                "[HPKata] Select rebar polylines and tags: ");

            using (var tr = _doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in ids)
                {
                    if (tr.GetObject(id, OpenMode.ForRead) is Entity ent)
                    {
                        list.Add(MapEntityToDto(ent, tr));
                    }
                }
                tr.Commit();
            }
            return list;
        }

        public void WriteMessage(string message)
        {
            _doc.Editor.WriteMessage(message);
        }

        public CadPromptResult PromptPoint(string message, out CadPoint3D point)
        {
            Point3d? res = CadSelectionService.PromptPoint(_doc.Editor, message);
            if (res.HasValue)
            {
                point = new CadPoint3D(res.Value.X, res.Value.Y, res.Value.Z);
                return CadPromptResult.Ok(point);
            }
            point = CadPoint3D.Origin;
            return CadPromptResult.Cancelled;
        }

        public CadPromptResult PromptString(string message, out string value)
        {
            string? res = CadSelectionService.PromptString(_doc.Editor, message);
            if (res != null)
            {
                value = res;
                return CadPromptResult.Ok(res);
            }
            value = string.Empty;
            return CadPromptResult.Cancelled;
        }

        public void InsertTable(CadTableDto table, CadPoint3D insertionPoint)
        {
            Database db = _doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
            var ms = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

            var cadTable = new Table();
            cadTable.SetSize(table.Rows.Count + 2, Math.Max(table.ColumnHeaders.Count, 1));
            cadTable.Position = new Point3d(insertionPoint.X, insertionPoint.Y, insertionPoint.Z);

            // Title
            cadTable.Cells[0, 0].TextString = table.Title;

            // Columns
            for (int col = 0; col < table.ColumnHeaders.Count; col++)
            {
                cadTable.Cells[1, col].TextString = table.ColumnHeaders[col];
            }

            // Rows
            for (int r = 0; r < table.Rows.Count; r++)
            {
                for (int c = 0; c < table.Rows[r].Cells.Count && c < table.ColumnHeaders.Count; c++)
                {
                    cadTable.Cells[r + 2, c].TextString = table.Rows[r].Cells[c];
                }
            }

            cadTable.GenerateLayout();
            ms.AppendEntity(cadTable);
            tr.AddNewlyCreatedDBObject(cadTable, true);
            tr.Commit();
        }

        public void UpdateEntityXData(string handle, KataXDataRecord record)
        {
            Database db = _doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            Entity? ent = HandleLinkService.FindLinkedEntity(db, handle, tr);
            if (ent != null)
            {
                ent.UpgradeOpen();
                XDataService.WriteXData(ent, record);
            }
            tr.Commit();
        }

        public void UpdateBlockAttributes(string handle, IReadOnlyDictionary<string, string> attributes)
        {
            Database db = _doc.Database;
            using var tr = db.TransactionManager.StartTransaction();
            Entity? ent = HandleLinkService.FindLinkedEntity(db, handle, tr);
            if (ent is BlockReference blk)
            {
                BlockAttributeService.UpdateAttributes(blk, attributes, tr);
            }
            tr.Commit();
        }

        public void HighlightEntity(string handle)
        {
            using var tr = _doc.Database.TransactionManager.StartTransaction();
            Entity? ent = HandleLinkService.FindLinkedEntity(_doc.Database, handle, tr);
            if (ent != null)
            {
                CadHighlightService.HighlightEntity(ent.ObjectId, tr);
            }
            tr.Commit();
        }

        public void ClearHighlights()
        {
            CadHighlightService.ClearAllHighlights(_doc.Database);
        }

        private static CadEntityDto MapEntityToDto(Entity ent, Transaction tr)
        {
            var dto = new CadEntityDto
            {
                Handle = ent.Handle.ToString(),
                Layer = ent.Layer ?? string.Empty,
                EntityType = ent.GetType().Name,
                XDataRecord = XDataService.ReadXData(ent)
            };

            if (ent is Polyline pl)
            {
                dto.TotalLengthMm = pl.Length;
                dto.ConstantWidth = pl.ConstantWidth;
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {
                    Point2d pt = pl.GetPoint2dAt(i);
                    dto.Vertices.Add(new CadPoint3D(pt.X, pt.Y, pl.Elevation));
                }
            }
            else if (ent is BlockReference blk)
            {
                dto.Attributes = BlockAttributeService.ReadAttributes(blk, tr);
            }

            return dto;
        }
    }
}
