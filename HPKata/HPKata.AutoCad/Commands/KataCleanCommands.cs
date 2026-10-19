using Autodesk.AutoCAD.Runtime;
using HPKata.AutoCad.Controllers;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// AutoCAD command methods for purging orphaned XRecords and temporary highlight markers (clean_kata, xoa_xrecord).
    /// </summary>
    public static class KataCleanCommands
    {
        [CommandMethod("clean_kata")]
        [CommandMethod("xoa_xrecord")]
        public static void CleanKata()
        {
            KataCleanController.CleanKataDatabase();
        }
    }
}
