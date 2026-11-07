using HPFoundation.Core.Sheets;

namespace HPFoundation.Core.Tests.Sheets;

/// <summary>Builds the anchors and inputs of a four-column sheet, shifted down so no address can be hard-coded.</summary>
internal static class FourColumnSheetSample
{
    public static SheetGrid Build(int rowShift = 0, Action<Dictionary<CellAddress, SheetCell>>? edit = null)
    {
        var cells = new Dictionary<CellAddress, SheetCell>();

        void Text(string address, string value) => cells[Shifted(address, rowShift)] = SheetCell.FromText(value);
        void Number(string address, double value) => cells[Shifted(address, rowShift)] = SheetCell.FromNumber(value);
        void Formula(string address, string formula, double value) =>
            cells[Shifted(address, rowShift)] = SheetCell.FromNumber(value) with { Formula = formula };

        Text("B15", "Tên trục");
        Text("C15", "Ntto");
        Text("D15", "Mttox");
        Text("E15", "Qttox");
        Text("F15", "Mttoy");
        Text("G15", "Qttoy");
        AddLoad(17, "A", 135, 1, 1, 2.7, 1.7);
        AddLoad(18, "B", 116, 1, 1, 3.4, 1.9);
        AddLoad(19, "C", 100.5, 1.1, 1.5, 3.1, 2.4);
        AddLoad(20, "D", 51.5, 1, 1, 2.6, 2.3);

        Text("A24", "Chọn chiều cao dầm móng h =");
        Number("D24", 0.8);
        Text("A25", "Chọn bề rộng móng b =");
        Number("D25", 2.4);

        Text("A27", "Console A");
        Text("F27", "x1");
        Number("A29", 1.6);
        Number("B29", 3.7);
        Number("C29", 3.7);
        Number("D29", 2.4);
        Number("E29", 0.3);

        Text("A32", " - Tổng mô men tính toán tại tâm móng");
        Formula("F34", "=ROUND(SUM(D17:D20)+ABS(C17*F29+C18*G29-C19*H29-C20*I29)+SUM(E17:E20)*D24,2)".Shift(rowShift), 42.85);
        Formula("D39", "=ROUND(SUM(F17:F20)+SUM(G17:G20)*D24,2)".Shift(rowShift), 18.44);

        Text("A68", "Chiều rộng tiết diện cột bc=");
        Number("D68", 0.35);
        Text("A69", "chọn bd =");
        Number("B69", 0.5);
        Text("A72", "Chiều cao cánh móng h1 =");
        Number("D72", 0.6);

        Text("A110", "- Cấp độ bền bê tông");
        Text("C110", "B20");
        Text("A111", "- Nhóm thép");
        Text("C111", "CB400-V");

        Text("B126", "BẢNG TÍNH HỆ SỐ LÒ XO ki");
        Text("F127", "ki(T)");
        Number("B128", 0.1);
        Number("F128", 159.72466);
        Number("B129", 0.05);
        Number("F129", 79.862329);

        edit?.Invoke(cells);
        return new SheetGrid("MB1-4COT", cells);

        void AddLoad(int row, string axis, double axial, double mx, double qx, double my, double qy)
        {
            Text($"B{row}", axis);
            Number($"C{row}", axial);
            Number($"D{row}", mx);
            Number($"E{row}", qx);
            Number($"F{row}", my);
            Number($"G{row}", qy);
        }
    }

    public static CellAddress At(string address, int rowShift = 0) => Shifted(address, rowShift);

    private static CellAddress Shifted(string address, int rowShift)
    {
        var parsed = CellAddress.Parse(address);
        return parsed with { Row = parsed.Row + rowShift };
    }

    private static string Shift(this string formula, int rowShift) =>
        System.Text.RegularExpressions.Regex.Replace(
            formula,
            @"(?<=[A-Z])\d+",
            match => (int.Parse(match.Value) + rowShift).ToString());
}
