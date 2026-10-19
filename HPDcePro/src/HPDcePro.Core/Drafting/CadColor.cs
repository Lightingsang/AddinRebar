using System;

namespace HPDcePro.Core.Drafting
{
    public readonly struct CadColor : IEquatable<CadColor>
    {
        public const int ByBlockIndex = 0;
        public const int RedIndex = 1;
        public const int YellowIndex = 2;
        public const int GreenIndex = 3;
        public const int CyanIndex = 4;
        public const int BlueIndex = 5;
        public const int MagentaIndex = 6;
        public const int WhiteIndex = 7;
        public const int ByLayerIndex = 256;

        public static CadColor ByLayer => new CadColor(ByLayerIndex);
        public static CadColor ByBlock => new CadColor(ByBlockIndex);
        public static CadColor Red => new CadColor(RedIndex);
        public static CadColor Yellow => new CadColor(YellowIndex);
        public static CadColor Green => new CadColor(GreenIndex);
        public static CadColor Cyan => new CadColor(CyanIndex);
        public static CadColor Blue => new CadColor(BlueIndex);
        public static CadColor Magenta => new CadColor(MagentaIndex);
        public static CadColor White => new CadColor(WhiteIndex);

        public int ColorIndex { get; }

        public CadColor(int colorIndex)
        {
            ColorIndex = (colorIndex >= 0 && colorIndex <= 256) ? colorIndex : ByLayerIndex;
        }

        public bool Equals(CadColor other) => ColorIndex == other.ColorIndex;
        public override bool Equals(object? obj) => obj is CadColor other && Equals(other);
        public override int GetHashCode() => ColorIndex;
        public override string ToString() => $"AciColor({ColorIndex})";

        public static bool operator ==(CadColor left, CadColor right) => left.Equals(right);
        public static bool operator !=(CadColor left, CadColor right) => !left.Equals(right);
    }
}
