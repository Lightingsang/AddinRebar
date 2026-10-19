using System.Collections.Generic;

namespace HPKata.Core.DrawingLayout.Models
{
    /// <summary>
    /// Table DTO representing the sorted, validated drawing index ready for AutoCAD or Revit table generation.
    /// </summary>
    public class DrawingIndexTableDto
    {
        public string Title { get; set; } = "DANH MỤC BẢN VẼ";
        public string[] Headers { get; set; } = { "STT", "KÝ HIỆU BẢN VẼ", "TÊN BẢN VẼ", "TỶ LỆ", "PHIÊN BẢN", "GHI CHÚ" };
        public List<string[]> Rows { get; set; } = new List<string[]>();
        public int TotalSheets => Rows.Count;
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
