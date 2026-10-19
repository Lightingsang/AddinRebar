namespace HPDcePro.Core.Calculations.Beam
{
    /// <summary>
    /// Standard concrete compressive strength classes per TCVN 5574:2018 Table 6.
    /// </summary>
    public enum ConcreteGrade
    {
        B15,
        B20,
        B25,
        B30,
        B35,
        B40
    }

    /// <summary>
    /// Design material properties for heavy concrete per TCVN 5574:2018 (Tables 6, 7, 10).
    /// </summary>
    public sealed class ConcreteProperties
    {
        public ConcreteGrade Grade { get; }
        public string Name => Grade.ToString();

        /// <summary>Design axial compressive strength Rb (MPa).</summary>
        public double Rb { get; }

        /// <summary>Design axial tensile strength Rbt (MPa).</summary>
        public double Rbt { get; }

        /// <summary>Initial modulus of elasticity Eb (MPa).</summary>
        public double Eb { get; }

        public ConcreteProperties(ConcreteGrade grade, double rb, double rbt, double eb)
        {
            Grade = grade;
            Rb = rb;
            Rbt = rbt;
            Eb = eb;
        }
    }
}
