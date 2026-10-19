using System.Windows.Controls;
using HPDcePro.UI.Common;

namespace HPDcePro.UI.Controls
{
    public partial class DcePaletteControl : UserControl
    {
        public DcePaletteControl()
        {
            ThemeManager.EnsureAssembliesLoaded();
            InitializeComponent();
        }
    }
}
