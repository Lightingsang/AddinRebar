using Autodesk.AutoCAD.Runtime;

namespace HPKata.AutoCad.Commands
{
    /// <summary>
    /// Command entry points for drawing, converting, and editing structural rebar entities.
    /// </summary>
    public static class RebarCommands
    {
        [CommandMethod("ve_thep")]
        [CommandMethod("vt")]
        public static void VeThep() => RebarDrawController.DrawRebarInteractive();

        [CommandMethod("taothep")]
        public static void TaoThep() => RebarDrawController.ConvertEntityToRebar();

        [CommandMethod("nhan_thep")]
        [CommandMethod("nhan")]
        public static void NhanThep() => RebarEditController.MultiplyBarCount();

        [CommandMethod("sua_thep")]
        [CommandMethod("sua")]
        public static void SuaThep() => RebarEditController.EditRebarInteractive();

        [CommandMethod("sua_moc_dai")]
        [CommandMethod("smd")]
        public static void SuaMocDai() => RebarEditController.ToggleStirrupHook();

        [CommandMethod("dai90")]
        public static void Dai90() => RebarEditController.SetStirrupHookAngle(90);

        [CommandMethod("dai135")]
        public static void Dai135() => RebarEditController.SetStirrupHookAngle(135);

        [CommandMethod("dai180")]
        public static void Dai180() => RebarEditController.SetStirrupHookAngle(180);

        [CommandMethod("dai90135")]
        public static void Dai90135() => RebarEditController.SetStirrupHookAngle(90135);

        [CommandMethod("neo_thep")]
        [CommandMethod("neo")]
        public static void NeoThep() => RebarEditController.AddAnchorHook();

        [CommandMethod("cat_thep")]
        [CommandMethod("cut")]
        public static void CatThep() => RebarEditController.SplitRebarWithLap();

        [CommandMethod("cat_tuy_y")]
        [CommandMethod("cattuyy")]
        public static void CatTuyY() => RebarEditController.SplitRebarArbitrary();

        [CommandMethod("dimthep")]
        public static void DimThep() => RebarEditController.DimensionRebarSegments();

        [CommandMethod("doi_mau_thep")]
        [CommandMethod("dmt")]
        public static void DoiMauThep() => RebarEditController.ColorByDiameter();

        [CommandMethod("dao_thep")]
        public static void DaoThep() => RebarEditController.ToggleStirrupHook();
    }
}
