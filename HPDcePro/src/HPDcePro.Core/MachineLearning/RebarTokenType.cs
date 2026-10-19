namespace HPDcePro.Core.MachineLearning
{
    /// <summary>
    /// Semantic token categories for Vietnamese structural rebar annotations.
    /// Matches the label schema in MayHoc/mauhoc.db.
    /// </summary>
    public enum RebarTokenType
    {
        Unknown = 0,
        SH = 1,     // Số hiệu (Bar mark, e.g., "1", "1b", "2a")
        SL = 2,     // Số lượng (Bar quantity multiplier, e.g., 2, 3)
        DK = 3,     // Đường kính (Bar diameter in mm: 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32)
        KC1 = 4,    // Đoạn uốn móc A (mm)
        KC2 = 5,    // Đoạn thân B (mm)
        KC3 = 6,    // Đoạn uốn móc C (mm)
        KC4 = 7,    // Đoạn uốn móc D (mm)
        L = 8,      // Chiều dài tổng L (mm)
        BO = 9,     // Bỏ qua (Extraneous or sheet index numbers)
        KC = 10,    // Khoảng cách rải cốt đai (Stirrup spacing, e.g., a150 -> 150)
        CD = 11     // Chiều dài đoạn thẳng đơn lẻ
    }
}
