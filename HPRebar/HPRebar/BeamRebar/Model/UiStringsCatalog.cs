namespace HPRebar.BeamRebar.Model;

/// <summary>
/// Preloaded English and Vietnamese catalogs for UI localization.
/// </summary>
public static class UiStringsCatalog
{
    public static UiStrings English { get; } = new() { LanguageToggle = "VN" };

    public static UiStrings Vietnamese { get; } = new()
    {
        WindowTitle = "Thép Dầm Liên Tục",
        Ok = "Thực Hiện",
        Cancel = "Hủy",
        LanguageToggle = "EN",
        Working = "Đang dựng cốt thép dầm liên tục...",
        NothingToCreate = "Không có gì để dựng với thiết lập hiện tại.",

        TabSettings = "Cài Đặt",
        TabGeometry = "Hình Dạng",
        TabStirrups = "Thép Đai",
        TabMainBars = "Thép Chủ",
        TabAddTopBars = "Thép Gối (Trên)",
        TabAddBottomBars = "Thép Bụng (Dưới)",
        TabSideBars = "Thép Giá / Cấu Tạo",
        TabSpecialBars = "Gia Cường Dầm Phụ",

        Span = "Nhịp",
        Spans = "Các Nhịp",
        Support = "Gối Tựa",
        Supports = "Các Gối Tựa",
        BarType = "Loại Thép",
        Diameter = "Đường Kính",
        Spacing = "Khoảng Cách",
        Count = "Số Thanh",
        Layer = "Lớp",
        Cover = "Lớp Bảo Vệ",
        Width = "Bề Rộng (b)",
        Height = "Chiều Cao (h)",
        Length = "Chiều Dài (L)",
        ClearSpan = "Thông Thủy (Ln)",
        HookLength = "Chiều Dài Móc Neo",
        LapLength = "Đoạn Nối Chồng",
        Partition = "Partition",
        ApplyAll = "Áp Dụng Cho Tất Cả",

        ViewGeneration = "Tự Động Tạo Khung Nhìn",
        CreateElevationView = "Tạo Chi Tiết Dọc Dầm",
        CreateSectionViews = "Tạo Mặt Cắt Ngang Dầm",
        CreateDimensions = "Ghi Kích Thước Dầm & Nhịp",
        CreateTags = "Tạo Bảng Thống Kê Thép Mặt Cắt",
        ElevationViewName = "Tên Chi Tiết Dọc",
        SectionViewPrefix = "Tiền Tố Mặt Cắt",

        BeamStackSummary = "Thông Số Dầm Liên Tục",
        SpanIndex = "Nhịp Số",
        DimensionsMm = "Kích Thước (mm)",
        Level = "Tầng / Level",
        TopElevation = "Cao Độ Đỉnh Dầm",
        Cantilever = "Công-xôn (Đầu Thừa)",

        StirrupLayout = "Quy Cách Rải Đai",
        LayoutUniform = "Rải Đều Toàn Bộ Nhịp",
        Layout3ZoneL4 = "3 Vùng: Gối L/4, Giữa Nhịp L/2",
        Layout3ZoneL3 = "3 Vùng: Gối L/3, Giữa Nhịp L/3",
        DenseSpacing = "Khoảng Cách Gối (s1)",
        MidspanSpacing = "Khoảng Cách Nhịp (s2)",
        StartOffset = "Khoảng Cách Đai Đầu Tiên (c0)",

        MainTopBars = "Thép Chủ Lớp Trên",
        MainBottomBars = "Thép Chủ Lớp Dưới",
        ContinuousBarCount = "Số Lượng Thanh Suốt",
        AnchorageHook = "Móc Neo 90° Tại Gối Biên",
        StaggeredSplice = "Nối Chồng So Le 50%",
        SpliceLength = "Chiều Dài Nối",

        AdditionalTopHeader = "Thép Tăng Cường Gối (Mô-men Âm)",
        SupportNode = "Vị Trí Gối",
        ExtensionRule = "Quy Cách Cắt Thép",
        RuleL3 = "Kéo Dài L/3 Nhịp Thông Thủy Lân Cận",
        RuleL4 = "Kéo Dài L/4 Nhịp Thông Thủy Lân Cận",

        AdditionalBottomHeader = "Thép Tăng Cường Bụng (Mô-men Dương)",
        MidspanOffsetRule = "Điểm Bắt Đầu Từ Mép Gối",
        RuleL7 = "Cách Mép Gối Ln/7",
        RuleL8 = "Cách Mép Gối Ln/8",

        SideBarsHeader = "Thép Cấu Tạo / Thép Giá (Thành Dầm)",
        EnableSideBars = "Bật Thép Cấu Tạo Thành Dầm",
        AutoDeepBeamRule = "Tự Động Khi Chiều Cao h >= 700 mm",
        MaxVerticalSpacing = "Khoảng Cách Dọc Tối Đa (<= 300 mm)",
        CrossTies = "Đai C Cố Định Thép Giá",

        SecondaryFramingHeader = "Gia Cường Vị Trí Giao Dầm Phụ",
        HangingStirrups = "Chùm Đai Treo",
        DiagonalTies = "Thanh Neo Xiên 45°",
        TieCount = "Số Lượng Đai"
    };
}
