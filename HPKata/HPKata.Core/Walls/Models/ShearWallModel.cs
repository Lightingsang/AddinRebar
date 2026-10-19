using System.Collections.Generic;

namespace HPKata.Core.Walls.Models
{
    public enum WallShapeType
    {
        Planar_I,
        L_Shape,
        T_Shape,
        C_Shape,
        Box_Core
    }

    /// <summary>
    /// Represents a reinforced concrete shear wall or elevator core wall with stories,
    /// boundary elements, web mesh, and openings.
    /// </summary>
    public sealed class ShearWallModel
    {
        public string WallMark { get; set; } = "V1";
        public WallShapeType ShapeType { get; set; } = WallShapeType.Planar_I;

        public double ThicknessMm { get; set; } = 300.0;
        public double LengthMm { get; set; } = 3000.0;
        public double ConcreteCoverMm { get; set; } = 25.0;
        public string ConcreteGrade { get; set; } = "B25";

        public List<WallStorySegment> Stories { get; set; } = new();
        public List<WallBoundaryElementModel> BoundaryElements { get; set; } = new();
        public WallWebReinforcementModel WebReinforcement { get; set; } = new();
        public List<WallOpeningModel> Openings { get; set; } = new();

        public double TotalHeightMm
        {
            get
            {
                if (Stories.Count == 0) return 0.0;
                return Stories[^1].TopElevationMm - Stories[0].BottomElevationMm;
            }
        }
    }
}
