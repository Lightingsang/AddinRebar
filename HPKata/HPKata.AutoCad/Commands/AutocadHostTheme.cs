using System;
using HPKata.UI.Common;
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Implements IHostTheme reading AutoCAD system variable 'COLORTHEME' (0 = Dark, 1 = Light).
    /// </summary>
    public sealed class AutocadHostTheme : IHostTheme
    {
        public static AutocadHostTheme Instance { get; } = new();

        private AutocadHostTheme()
        {
            try
            {
                AcadApp.SystemVariableChanged += (_, args) =>
                {
                    if (string.Equals(args.Name, "COLORTHEME", StringComparison.OrdinalIgnoreCase))
                    {
                        Changed?.Invoke();
                    }
                };
            }
            catch (Exception)
            {
                // Silently fallback if running outside active AutoCAD application context
            }
        }

        public bool IsDark
        {
            get
            {
                try
                {
                    return Convert.ToInt32(AcadApp.GetSystemVariable("COLORTHEME")) == 0;
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        public event Action? Changed;
    }
}
