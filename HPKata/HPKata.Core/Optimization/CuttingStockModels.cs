using System;
using System.Collections.Generic;
using System.Linq;

namespace HPKata.Core.Optimization
{
    public enum SolverStrategy
    {
        Auto = 0,
        DuBang0 = 1,
        LapDayCanhDai = 2,
        BeamSearchTopK = 3,
        PatternCat = 4
    }

    public class CuttingDemand
    {
        public int DiameterMm { get; set; }
        public double LengthMm { get; set; }
        public int Quantity { get; set; }
        public string BarMark { get; set; } = string.Empty;
        public string ComponentMark { get; set; } = string.Empty;

        public CuttingDemand()
        {
        }

        public CuttingDemand(int diameterMm, double lengthMm, int quantity, string barMark = "", string componentMark = "")
        {
            DiameterMm = diameterMm;
            LengthMm = lengthMm;
            Quantity = quantity;
            BarMark = barMark;
            ComponentMark = componentMark;
        }
    }

    public class CuttingPattern
    {
        public double StockLengthMm { get; set; }
        public bool IsWarehouseBar { get; set; }
        public List<double> CutsMm { get; set; } = new();
        public double KerfMm { get; set; }
        public double UsedLengthMm => CutsMm.Sum() + (CutsMm.Count > 1 ? (CutsMm.Count - 1) * KerfMm : 0.0);
        public double RemnantLengthMm => Math.Max(0.0, StockLengthMm - UsedLengthMm);
        public bool IsReusableRemnant { get; set; }

        public override string ToString() =>
            $"Stock {StockLengthMm:F0}mm: [{string.Join(" + ", CutsMm.Select(c => c.ToString("F0")))}] Rem={RemnantLengthMm:F0}mm";
    }

    public class CuttingStockResult
    {
        public int DiameterMm { get; set; }
        public List<CuttingPattern> Patterns { get; set; } = new();
        public int NewStandardBarsCount => Patterns.Count(p => !p.IsWarehouseBar);
        public int ConsumedWarehouseBarsCount => Patterns.Count(p => p.IsWarehouseBar);
        public List<Models.WarehouseOffcut> NewWarehouseOffcuts { get; set; } = new();
        public double GrossWastePercent { get; set; }
        public double NetWastePercent { get; set; }
        public int TotalPiecesCut => Patterns.Sum(p => p.CutsMm.Count);
        public SolverStrategy StrategyUsed { get; set; }
    }
}
