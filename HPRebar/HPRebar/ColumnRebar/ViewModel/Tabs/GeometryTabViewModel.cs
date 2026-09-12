using HPRebar.ColumnRebar.ViewModel;
using HPRebar.ColumnRebar.Service;

namespace HPRebar.ColumnRebar.ViewModel.Tabs;

/// <summary>Read-only view of what was measured off the model, so the user can sanity-check the pick.</summary>
public sealed partial class GeometryTabViewModel : ColumnRebarTabViewModel
{
    public GeometryTabViewModel(ColumnRebarSession session, LocalizationService localization)
        : base(session, localization)
    {
    }

    public override string Title => Localization.Strings.TabGeometry;

    public override string IconKey => "TabIcon.Geometry";

    public string StyleName => Session.Stack.Style.ToString();

    public string FamilyName =>
        Session.Stack.Faces.Count == 0 ? string.Empty : Session.Stack.Faces[0].Element.Name;
}
