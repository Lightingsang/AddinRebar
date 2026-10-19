using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace HPKata.AutoCad.Interop
{
    /// <summary>
    /// Safe user selection and prompt services wrapping AutoCAD Editor operations.
    /// </summary>
    public static class CadSelectionService
    {
        public static ObjectId? PromptSelectEntity(
            Editor ed,
            string message,
            Type? allowedType = null)
        {
            if (ed == null) throw new ArgumentNullException(nameof(ed));

            var opt = new PromptEntityOptions($"\n{message}");
            if (allowedType != null)
            {
                opt.SetRejectMessage($"\n[HPKata] Selected entity must be of type {allowedType.Name}.");
                opt.AddAllowedClass(allowedType, exactMatch: false);
            }

            PromptEntityResult res = ed.GetEntity(opt);
            return res.Status == PromptStatus.OK ? res.ObjectId : null;
        }

        public static List<ObjectId> PromptSelectMultiple(
            Editor ed,
            string message,
            SelectionFilter? filter = null)
        {
            var ids = new List<ObjectId>();
            if (ed == null) return ids;

            ed.WriteMessage($"\n{message}");
            PromptSelectionResult res = filter != null ? ed.GetSelection(filter) : ed.GetSelection();

            if (res.Status == PromptStatus.OK && res.Value != null)
            {
                ids.AddRange(res.Value.GetObjectIds());
            }
            return ids;
        }

        public static Point3d? PromptPoint(Editor ed, string message)
        {
            if (ed == null) return null;

            var opt = new PromptPointOptions($"\n{message}");
            PromptPointResult res = ed.GetPoint(opt);
            return res.Status == PromptStatus.OK ? res.Value : null;
        }

        public static string? PromptString(Editor ed, string message, string defaultValue = "")
        {
            if (ed == null) return null;

            var opt = new PromptStringOptions($"\n{message}")
            {
                AllowSpaces = true,
                DefaultValue = defaultValue,
                UseDefaultValue = !string.IsNullOrEmpty(defaultValue)
            };

            PromptResult res = ed.GetString(opt);
            return res.Status == PromptStatus.OK ? res.StringResult : null;
        }

        public static SelectionFilter CreateRebarFilter()
        {
            var tvs = new TypedValue[]
            {
                new((int)DxfCode.Start, "LWPOLYLINE"),
                new((int)DxfCode.ExtendedDataRegAppName, "KATA")
            };
            return new SelectionFilter(tvs);
        }
    }
}
