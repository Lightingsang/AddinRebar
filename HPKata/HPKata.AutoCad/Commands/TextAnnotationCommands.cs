using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// AutoCAD command methods for text alignment (dtext) and section elevation callouts (mcat).
    /// </summary>
    public static class TextAnnotationCommands
    {
        [CommandMethod("dtext")]
        [CommandMethod("can_chinh_text")]
        public static void AlignText()
        {
            TextAnnotationController.AlignTextInteractive();
        }

        [CommandMethod("mcat")]
        [CommandMethod("cao_do_mat_cat")]
        public static void CreateElevationCallout()
        {
            TextAnnotationController.CreateElevationCalloutInteractive();
        }

        [CommandMethod("dten")]
        public static void NumberComponentMarks()
        {
            TextAnnotationController.NumberComponentMarksInteractive();
        }

        [CommandMethod("tkten")]
        public static void GenerateComponentTable()
        {
            TextAnnotationController.GenerateComponentTableInteractive();
        }

        [CommandMethod("tim_ten_ck")]
        public static void FindComponentMark()
        {
            TextAnnotationController.FindComponentMarkInteractive();
        }

        [CommandMethod("textblock")]
        public static void ConvertTextToBlock()
        {
            TextAnnotationController.ConvertTextToBlockInteractive();
        }
    }
}
