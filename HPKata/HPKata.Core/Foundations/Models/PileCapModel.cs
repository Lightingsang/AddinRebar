using System;
using System.Collections.Generic;

namespace HPKata.Core.Foundations.Models
{
    /// <summary>
    /// Represents a pile cap (đài cọc) foundation, supporting 1 to 9+ piles, single or stepped tier.
    /// </summary>
    public class PileCapModel
    {
        public string CapMark { get; set; } = "ĐC-1";
        public double LengthMm { get; set; } = 2200.0;
        public double WidthMm { get; set; } = 2200.0;
        public double TotalHeightMm { get; set; } = 1000.0;

        public bool IsStepped { get; set; }
        public double Tier2LengthMm { get; set; } = 1400.0;
        public double Tier2WidthMm { get; set; } = 1400.0;
        public double Tier2HeightMm { get; set; } = 400.0;
        public double BaseTierHeightMm => IsStepped ? Math.Max(300.0, TotalHeightMm - Tier2HeightMm) : TotalHeightMm;

        public List<PileCoordinate> Piles { get; set; } = new();
        public double PileDiameterMm { get; set; } = 400.0;
        public bool IsPileSquare { get; set; }
        public double PileEmbedmentDepthMm { get; set; } = 100.0;

        public double CoverBottomMm { get; set; } = 50.0;
        public double CoverSideMm { get; set; } = 50.0;
        public double CoverTopMm { get; set; } = 50.0;

        public int BottomMeshXDiaMm { get; set; } = 18;
        public double BottomMeshXSpacingMm { get; set; } = 150.0;
        public int BottomMeshYDiaMm { get; set; } = 18;
        public double BottomMeshYSpacingMm { get; set; } = 150.0;

        public bool HasTopMesh { get; set; }
        public int TopMeshXDiaMm { get; set; } = 14;
        public double TopMeshXSpacingMm { get; set; } = 200.0;
        public int TopMeshYDiaMm { get; set; } = 14;
        public double TopMeshYSpacingMm { get; set; } = 200.0;

        public bool HasSkinTies { get; set; } = true;
        public int SkinTieDiaMm { get; set; } = 12;
        public double SkinTieSpacingMm { get; set; } = 200.0;

        public int StarterDowelCount { get; set; } = 8;
        public int StarterDowelDiaMm { get; set; } = 20;
        public double StarterDowelLapMm { get; set; } = 800.0;

        public int PileConfinementCount { get; set; } = 3;
        public int PileConfinementDiaMm { get; set; } = 8;

        public int MemberCount { get; set; } = 1;
        public string ConcreteGrade { get; set; } = "B25";
        public string SteelGrade { get; set; } = "CB400-V";

        public void Validate()
        {
            if (LengthMm <= 0 || WidthMm <= 0 || TotalHeightMm <= 0)
                throw new ArgumentException("Cap dimensions must be positive.");
            if (IsStepped)
            {
                if (Tier2LengthMm >= LengthMm || Tier2WidthMm >= WidthMm)
                    throw new ArgumentException("Upper tier dimensions cannot exceed base tier dimensions.");
                if (Tier2HeightMm >= TotalHeightMm)
                    throw new ArgumentException("Upper tier height must be less than total cap height.");
            }
            if (BottomMeshXSpacingMm <= 0 || BottomMeshYSpacingMm <= 0)
                throw new ArgumentException("Bottom mesh spacings must be positive.");
            if (MemberCount < 1) MemberCount = 1;
        }
    }
}
