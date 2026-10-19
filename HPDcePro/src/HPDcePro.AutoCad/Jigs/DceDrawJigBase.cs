using Autodesk.AutoCAD.EditorInput;

namespace HPDcePro.AutoCad.Jigs
{
    /// <summary>
    /// Base class for all 11 HPDcePro interactive CAD DrawJigs.
    /// Provides standardized StartJig lifecycle and decoupled geometry calculation hooks.
    /// </summary>
    public abstract class DceDrawJigBase : DrawJig
    {
        public abstract PromptStatus StartJig(Editor editor);
    }
}
