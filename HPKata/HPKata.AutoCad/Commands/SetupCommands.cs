using Autodesk.AutoCAD.Runtime;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for master configuration and rebar detailing setup.
    /// </summary>
    public static class SetupCommands
    {
        [CommandMethod("setupkata")]
        public static void SetupKata()
        {
            SetupController.OpenSetupView(isGeneralSetup: true);
        }

        [CommandMethod("setupkata1")]
        public static void SetupKata1()
        {
            SetupController.OpenSetupView(isGeneralSetup: true);
        }

        [CommandMethod("setupthep")]
        public static void SetupThep()
        {
            SetupController.OpenSetupView(isGeneralSetup: false);
        }
    }
}
