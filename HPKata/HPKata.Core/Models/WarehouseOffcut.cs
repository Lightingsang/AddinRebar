namespace HPKata.Core.Models
{
    /// <summary>
    /// Represents an offcut piece in warehouse inventory (Kho_thep).
    /// </summary>
    public class WarehouseOffcut
    {
        public int DiameterMm { get; set; }
        public double LengthMm { get; set; }
        public int Quantity { get; set; }

        public WarehouseOffcut()
        {
        }

        public WarehouseOffcut(int diameterMm, double lengthMm, int quantity)
        {
            DiameterMm = diameterMm;
            LengthMm = lengthMm;
            Quantity = quantity;
        }

        public override string ToString() =>
            $"D{DiameterMm} L={LengthMm:F0}mm (x{Quantity})";
    }
}
