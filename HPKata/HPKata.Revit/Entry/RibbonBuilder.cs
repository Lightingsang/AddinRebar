using System.Reflection;
using System.Windows.Media;
using Autodesk.Revit.UI;

namespace HPKata.Revit.Entry;

/// <summary>
/// Builds and manages the HPKATA ribbon tab, panel, and buttons in Revit 2026.
/// Supports dynamic Dark/Light icon updates on theme change.
/// </summary>
public sealed class RibbonBuilder
{
    public const string TabName = "HPKata";
    public const string PanelName = "Cốt Thép (Rebar)";
    public const string FramingColumnPanelName = "Dầm, Cột & Vách";
    public const string FoundationSlabPanelName = "Móng, Sàn & Thang";
    public const string QsLayoutPanelName = "QS & Bản Vẽ";

    private readonly List<(PushButton Button, Func<bool, ImageSource> Provider)> _buttons = [];

    public void Build(UIControlledApplication app)
    {
        CreateTabSafe(app, TabName);
        string assemblyPath = Assembly.GetExecutingAssembly().Location;

        BuildCorePanel(app, assemblyPath);
        BuildFramingAndColumnPanel(app, assemblyPath);
        BuildFoundationAndSlabPanel(app, assemblyPath);
        BuildQsAndLayoutPanel(app, assemblyPath);

        UpdateIcons(RevitHostTheme.Instance.IsDark);
    }

    private void BuildCorePanel(UIControlledApplication app, string assemblyPath)
    {
        var panel = CreatePanelSafe(app, TabName, PanelName);
        AddButton(panel, "HPKata_Setup", "Thiết Lập\nKata", "HPKata.Revit.Commands.SetupCommand",
            "Cấu hình tham số và tiêu chuẩn uốn móc cốt thép KataPro",
            RibbonIconFactory.CreateSetupIcon, assemblyPath);
        AddButton(panel, "HPKata_Detailing", "Vẽ & Sửa\nThép", "HPKata.Revit.Commands.DetailingCommand",
            "Vẽ, điều chỉnh và quản lý hình dạng thanh cốt thép Kata",
            RibbonIconFactory.CreateDetailingIcon, assemblyPath);
        AddButton(panel, "HPKata_Schedule", "Thống Kê\nCốt Thép", "HPKata.Revit.Commands.ScheduleCommand",
            "Lập bảng thống kê thép và tổng hợp theo đường kính",
            RibbonIconFactory.CreateScheduleIcon, assemblyPath);
        AddButton(panel, "HPKata_CuttingStock", "Tổ Hợp\nCắt Thép", "HPKata.Revit.Commands.CuttingStockCommand",
            "Tối ưu hóa tổ hợp cắt thanh thương mại 11.7m giảm phế liệu",
            RibbonIconFactory.CreateCuttingStockIcon, assemblyPath);
        AddButton(panel, "HPKata_ExportExcel", "Xuất\nExcel", "HPKata.Revit.Commands.ExportExcelCommand",
            "Xuất bảng thống kê cốt thép và mối nối coupler ra Excel (ClosedXML)",
            RibbonIconFactory.CreateExportExcelIcon, assemblyPath);
    }

    private void BuildFramingAndColumnPanel(UIControlledApplication app, string assemblyPath)
    {
        var panel = CreatePanelSafe(app, TabName, FramingColumnPanelName);
        AddButton(panel, "HPKata_FramingRebar", "Thép\nDầm", "HPKata.Revit.Commands.FramingRebarCommands",
            "Triển khai cốt thép dầm liên tục, đai treo và cắt thép nhịp/gối",
            RibbonIconFactory.CreateBeamIcon, assemblyPath);
        AddButton(panel, "HPKata_EtabsFraming", "ETABS\nDầm", "HPKata.Revit.Commands.EtabsFramingImportCommand",
            "Nhập diện tích cốt thép dầm từ ETABS và bố trí tự động theo TCVN",
            RibbonIconFactory.CreateEtabsIcon, assemblyPath);
        AddButton(panel, "HPKata_ColumnRebar", "Thép\nCột", "HPKata.Revit.Commands.ColumnRebarCommands",
            "Triển khai cốt thép cột đa tầng, nhấn cổ chai 1:6 và nối chồng so le",
            RibbonIconFactory.CreateColumnWallIcon, assemblyPath);
        AddButton(panel, "HPKata_WallRebar", "Thép\nVách", "HPKata.Revit.Commands.WallRebarCommands",
            "Triển khai cốt thép vách cứng, vùng biên chịu uốn và gia cường lỗ mở",
            RibbonIconFactory.CreateColumnWallIcon, assemblyPath);
        AddButton(panel, "HPKata_EtabsColWall", "ETABS\nCột/Vách", "HPKata.Revit.Commands.EtabsColumnWallImportCommand",
            "Nhập kết quả thiết kế cột và vách từ ETABS",
            RibbonIconFactory.CreateEtabsIcon, assemblyPath);
    }

    private void BuildFoundationAndSlabPanel(UIControlledApplication app, string assemblyPath)
    {
        var panel = CreatePanelSafe(app, TabName, FoundationSlabPanelName);
        AddButton(panel, "HPKata_FoundationRebar", "Thép\nMóng", "HPKata.Revit.Commands.FoundationRebarCommands",
            "Triển khai cốt thép móng đơn, đài cọc và đài cọc giật cấp",
            RibbonIconFactory.CreateFoundationIcon, assemblyPath);
        AddButton(panel, "HPKata_SlabRebar", "Thép\nSàn", "HPKata.Revit.Commands.SlabRebarCommands",
            "Triển khai cốt thép sàn 2 phương, thép mũ gối và chân chó",
            RibbonIconFactory.CreateSlabIcon, assemblyPath);
        AddButton(panel, "HPKata_StairRebar", "Thép\nThang", "HPKata.Revit.Commands.StairRebarCommands",
            "Triển khai cốt thép bản thang, chiếu nghỉ và lanh tô",
            RibbonIconFactory.CreateStairIcon, assemblyPath);
    }

    private void BuildQsAndLayoutPanel(UIControlledApplication app, string assemblyPath)
    {
        var panel = CreatePanelSafe(app, TabName, QsLayoutPanelName);
        AddButton(panel, "HPKata_RevitQs", "Bóc Tách\nQS", "HPKata.Revit.Commands.RevitQsCommands",
            "Bóc tách khối lượng bê tông và cốp pha có khấu trừ giao cắt 3D",
            RibbonIconFactory.CreateQsIcon, assemblyPath);
        AddButton(panel, "HPKata_RevitLayout", "Dàn Trang\nBản Vẽ", "HPKata.Revit.Commands.RevitLayoutCommands",
            "Tự động xếp các View lên Sheet A1/A3 theo giải thuật Shelf-Packing 2D",
            RibbonIconFactory.CreateLayoutIcon, assemblyPath);
    }

    public void UpdateIcons(bool isDark)
    {
        foreach (var (button, provider) in _buttons)
        {
            var icon = provider(isDark);
            button.LargeImage = icon;
            button.Image = icon;
        }
    }

    private void AddButton(
        RibbonPanel panel,
        string internalName,
        string displayText,
        string commandClassName,
        string toolTip,
        Func<bool, ImageSource> iconProvider,
        string assemblyPath)
    {
        var data = new PushButtonData(internalName, displayText, assemblyPath, commandClassName)
        {
            ToolTip = toolTip
        };

        if (panel.AddItem(data) is PushButton pushButton)
        {
            _buttons.Add((pushButton, iconProvider));
        }
    }

    private static void CreateTabSafe(UIControlledApplication app, string tab)
    {
        try
        {
            app.CreateRibbonTab(tab);
        }
        catch (Autodesk.Revit.Exceptions.ArgumentException)
        {
            // Expected when the tab is already created by another add-in module
        }
    }

    private static RibbonPanel CreatePanelSafe(UIControlledApplication app, string tab, string panel)
    {
        var existingPanels = app.GetRibbonPanels(tab);
        foreach (var p in existingPanels)
        {
            if (string.Equals(p.Name, panel, StringComparison.OrdinalIgnoreCase))
            {
                return p;
            }
        }
        return app.CreateRibbonPanel(tab, panel);
    }
}
