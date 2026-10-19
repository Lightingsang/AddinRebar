namespace HPKata.Core.DrawingLayout.Models
{
    /// <summary>
    /// Represents an entry in the master drawing index (Danh mục bản vẽ).
    /// </summary>
    public class DrawingIndexItem
    {
        public int Order { get; set; }
        public string SheetNumber { get; set; } = string.Empty;
        public string DrawingTitle { get; set; } = string.Empty;
        public string Scale { get; set; } = "1:100";
        public string Revision { get; set; } = "00";
        public string Date { get; set; } = string.Empty;
        public string Category { get; set; } = "Kết cấu";

        public DrawingIndexItem()
        {
        }

        public DrawingIndexItem(int order, string sheetNum, string title, string scale = "1:100")
        {
            Order = order;
            SheetNumber = sheetNum;
            DrawingTitle = title;
            Scale = scale;
        }
    }
}
