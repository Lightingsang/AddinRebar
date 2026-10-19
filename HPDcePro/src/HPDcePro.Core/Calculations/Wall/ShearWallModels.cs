using System;
using HPDcePro.Core.Calculations.Beam;

namespace HPDcePro.Core.Calculations.Wall
{
    public sealed class ShearWallInput
    {
        public string WallMark { get; }
        public double LengthLwMm { get; }
        public double ThicknessTwMm { get; }
        public double HeightHwMm { get; }
        public ConcreteProperties Concrete { get; }
        public RebarProperties Rebar { get; }
        public double DesignAxialNKn { get; }
        public double DesignShearVuKn { get; }
        public double DesignMomentMuKnm { get; }
        public int WebVerticalBarDiameter { get; }
        public double WebVerticalBarSpacingMm { get; }
        public int WebHorizontalBarDiameter { get; }
        public double WebHorizontalBarSpacingMm { get; }
        public int CrossTiesPerM2 { get; }

        public ShearWallInput(
            string wallMark,
            double lengthLwMm,
            double thicknessTwMm,
            double heightHwMm,
            ConcreteProperties concrete,
            RebarProperties rebar,
            double designAxialNKn,
            double designShearVuKn,
            double designMomentMuKnm,
            int webVerticalBarDiameter = 12,
            double webVerticalBarSpacingMm = 200.0,
            int webHorizontalBarDiameter = 10,
            double webHorizontalBarSpacingMm = 150.0,
            int crossTiesPerM2 = 4)
        {
            WallMark = string.IsNullOrWhiteSpace(wallMark) ? "W1" : wallMark;
            LengthLwMm = lengthLwMm > 0 ? lengthLwMm : throw new ArgumentException("Wall length must be positive.", nameof(lengthLwMm));
            ThicknessTwMm = thicknessTwMm >= 150.0 ? thicknessTwMm : throw new ArgumentException("Wall thickness must be at least 150 mm.", nameof(thicknessTwMm));
            HeightHwMm = heightHwMm > 0 ? heightHwMm : 3000.0;
            Concrete = concrete ?? throw new ArgumentNullException(nameof(concrete));
            Rebar = rebar ?? throw new ArgumentNullException(nameof(rebar));
            DesignAxialNKn = designAxialNKn;
            DesignShearVuKn = designShearVuKn;
            DesignMomentMuKnm = designMomentMuKnm;
            WebVerticalBarDiameter = webVerticalBarDiameter;
            WebVerticalBarSpacingMm = webVerticalBarSpacingMm;
            WebHorizontalBarDiameter = webHorizontalBarDiameter;
            WebHorizontalBarSpacingMm = webHorizontalBarSpacingMm;
            CrossTiesPerM2 = crossTiesPerM2;
        }
    }

    public sealed class BoundaryElementResult
    {
        public bool IsRequired { get; }
        public double LengthLcMm { get; }
        public double WidthBcMm { get; }
        public int RecommendedBarCount { get; }
        public int RecommendedBarDiameter { get; }
        public double ProvidedAreaAsMm2 { get; }
        public double RebarRatioPercent { get; }
        public int TieDiameterMm { get; }
        public double TieSpacingMm { get; }

        public BoundaryElementResult(
            bool isRequired,
            double lengthLcMm,
            double widthBcMm,
            int recommendedBarCount,
            int recommendedBarDiameter,
            double providedAreaAsMm2,
            double rebarRatioPercent,
            int tieDiameterMm,
            double tieSpacingMm)
        {
            IsRequired = isRequired;
            LengthLcMm = lengthLcMm;
            WidthBcMm = widthBcMm;
            RecommendedBarCount = recommendedBarCount;
            RecommendedBarDiameter = recommendedBarDiameter;
            ProvidedAreaAsMm2 = providedAreaAsMm2;
            RebarRatioPercent = rebarRatioPercent;
            TieDiameterMm = tieDiameterMm;
            TieSpacingMm = tieSpacingMm;
        }
    }

    public sealed class WebReinforcementResult
    {
        public double VerticalRatioPercent { get; }
        public double HorizontalRatioPercent { get; }
        public int CurtainCount { get; }
        public int CrossTieDensityPerM2 { get; }
        public bool IsSufficient { get; }

        public WebReinforcementResult(
            double verticalRatioPercent,
            double horizontalRatioPercent,
            int curtainCount,
            int crossTieDensityPerM2,
            bool isSufficient)
        {
            VerticalRatioPercent = verticalRatioPercent;
            HorizontalRatioPercent = horizontalRatioPercent;
            CurtainCount = curtainCount;
            CrossTieDensityPerM2 = crossTieDensityPerM2;
            IsSufficient = isSufficient;
        }
    }

    public sealed class ShearWallResult
    {
        public string WallMark { get; }
        public double MaxCompressiveStressMpa { get; }
        public double StressThresholdMpa { get; }
        public bool BoundaryElementsRequired { get; }
        public BoundaryElementResult BoundaryElement { get; }
        public WebReinforcementResult WebReinforcement { get; }
        public double ConcreteShearCapacityVbKn { get; }
        public double WebShearCapacityVswKn { get; }
        public double TotalShearCapacityVuMaxKn { get; }
        public double ShearUtilizationRatio { get; }
        public bool IsShearSafe { get; }
        public bool IsOverallSafe { get; }
        public string Summary { get; }

        public ShearWallResult(
            string wallMark,
            double maxCompressiveStressMpa,
            double stressThresholdMpa,
            bool boundaryElementsRequired,
            BoundaryElementResult boundaryElement,
            WebReinforcementResult webReinforcement,
            double concreteShearCapacityVbKn,
            double webShearCapacityVswKn,
            double totalShearCapacityVuMaxKn,
            double shearUtilizationRatio,
            bool isShearSafe,
            bool isOverallSafe,
            string summary)
        {
            WallMark = wallMark;
            MaxCompressiveStressMpa = maxCompressiveStressMpa;
            StressThresholdMpa = stressThresholdMpa;
            BoundaryElementsRequired = boundaryElementsRequired;
            BoundaryElement = boundaryElement;
            WebReinforcement = webReinforcement;
            ConcreteShearCapacityVbKn = concreteShearCapacityVbKn;
            WebShearCapacityVswKn = webShearCapacityVswKn;
            TotalShearCapacityVuMaxKn = totalShearCapacityVuMaxKn;
            ShearUtilizationRatio = shearUtilizationRatio;
            IsShearSafe = isShearSafe;
            IsOverallSafe = isOverallSafe;
            Summary = summary;
        }
    }
}
