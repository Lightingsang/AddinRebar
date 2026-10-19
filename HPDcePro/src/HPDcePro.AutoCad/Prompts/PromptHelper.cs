using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace HPDcePro.AutoCad.Prompts
{
    /// <summary>
    /// Safe wrapper for interactive AutoCAD Editor prompt operations with headless execution fallback.
    /// </summary>
    public static class PromptHelper
    {
        public static Document? GetActiveDocument()
        {
            try
            {
                return Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager?.MdiActiveDocument;
            }
            catch (Exception)
            {
                // Reason: Outside acad.exe process (e.g. unit tests or headless runners), AutoCAD DocumentManager throws InvalidProgramException.
                return null;
            }
        }

        public static Editor? GetActiveEditor()
        {
            try
            {
                return GetActiveDocument()?.Editor;
            }
            catch (Exception)
            {
                // Reason: Outside acad.exe process (e.g. unit tests or headless runners), AutoCAD DocumentManager throws InvalidProgramException.
                return null;
            }
        }

        public static bool TryGetPoint(Editor? ed, string prompt, out Point3d point, Point3d? basePoint = null)
        {
            point = default;
            if (ed == null) return false;

            try
            {
                var options = new PromptPointOptions("\n" + prompt);
                if (basePoint.HasValue)
                {
                    options.BasePoint = basePoint.Value;
                    options.UseBasePoint = true;
                }

                var res = ed.GetPoint(options);
                if (res.Status == PromptStatus.OK)
                {
                    point = res.Value;
                    return true;
                }
            }
            catch (Exception)
            {
                // Reason: Headless test runner or AutoCAD document without active console context.
                return false;
            }

            return false;
        }

        public static bool TryGetDouble(Editor? ed, string prompt, double defaultValue, out double val)
        {
            val = defaultValue;
            if (ed == null) return false;

            try
            {
                var options = new PromptDoubleOptions($"\n{prompt} <{defaultValue}>: ")
                {
                    DefaultValue = defaultValue,
                    UseDefaultValue = true
                };

                var res = ed.GetDouble(options);
                if (res.Status == PromptStatus.OK)
                {
                    val = res.Value;
                    return true;
                }
            }
            catch (Exception)
            {
                // Reason: Headless execution fallback.
                return false;
            }

            return false;
        }

        public static bool TryGetInteger(Editor? ed, string prompt, int defaultValue, out int val)
        {
            val = defaultValue;
            if (ed == null) return false;

            try
            {
                var options = new PromptIntegerOptions($"\n{prompt} <{defaultValue}>: ")
                {
                    DefaultValue = defaultValue,
                    UseDefaultValue = true
                };

                var res = ed.GetInteger(options);
                if (res.Status == PromptStatus.OK)
                {
                    val = res.Value;
                    return true;
                }
            }
            catch (Exception)
            {
                // Reason: Headless execution fallback.
                return false;
            }

            return false;
        }

        public static bool TryGetString(Editor? ed, string prompt, string defaultValue, out string val)
        {
            val = defaultValue;
            if (ed == null) return false;

            try
            {
                var options = new PromptStringOptions($"\n{prompt} <{defaultValue}>: ")
                {
                    DefaultValue = defaultValue,
                    UseDefaultValue = true,
                    AllowSpaces = true
                };

                var res = ed.GetString(options);
                if (res.Status == PromptStatus.OK)
                {
                    val = res.StringResult;
                    return true;
                }
            }
            catch (Exception)
            {
                // Reason: Headless execution fallback.
                return false;
            }

            return false;
        }

        public static bool TryGetKeywords(Editor? ed, string prompt, string[] keywords, string defaultKeyword, out string selected)
        {
            selected = defaultKeyword;
            if (ed == null || keywords == null || keywords.Length == 0) return false;

            try
            {
                var options = new PromptKeywordOptions($"\n{prompt}")
                {
                    AllowNone = true
                };

                for (int i = 0; i < keywords.Length; i++)
                {
                    options.Keywords.Add(keywords[i]);
                }

                if (!string.IsNullOrEmpty(defaultKeyword))
                {
                    options.Keywords.Default = defaultKeyword;
                }

                var res = ed.GetKeywords(options);
                if (res.Status == PromptStatus.OK)
                {
                    selected = res.StringResult;
                    return true;
                }
            }
            catch (Exception)
            {
                // Reason: Headless execution fallback.
                return false;
            }

            return false;
        }

        public static bool TryGetEntity(Editor? ed, string prompt, Type[]? allowedTypes, out ObjectId entityId)
        {
            entityId = ObjectId.Null;
            if (ed == null) return false;

            try
            {
                var options = new PromptEntityOptions("\n" + prompt);
                if (allowedTypes != null && allowedTypes.Length > 0)
                {
                    for (int i = 0; i < allowedTypes.Length; i++)
                    {
                        options.SetRejectMessage($"\nSelected entity must be of type {allowedTypes[i].Name}.");
                        options.AddAllowedClass(allowedTypes[i], exactMatch: false);
                    }
                }

                var res = ed.GetEntity(options);
                if (res.Status == PromptStatus.OK)
                {
                    entityId = res.ObjectId;
                    return true;
                }
            }
            catch (Exception)
            {
                // Reason: Headless execution fallback.
                return false;
            }

            return false;
        }

        public static bool TryGetSelection(Editor? ed, string prompt, SelectionFilter? filter, out SelectionSet? selection)
        {
            selection = null;
            if (ed == null) return false;

            try
            {
                var options = new PromptSelectionOptions
                {
                    MessageForAdding = "\n" + prompt
                };

                PromptSelectionResult res = filter != null
                    ? ed.GetSelection(options, filter)
                    : ed.GetSelection(options);

                if (res.Status == PromptStatus.OK && res.Value != null)
                {
                    selection = res.Value;
                    return true;
                }
            }
            catch (Exception)
            {
                // Reason: Headless execution fallback.
                return false;
            }

            return false;
        }
    }
}
