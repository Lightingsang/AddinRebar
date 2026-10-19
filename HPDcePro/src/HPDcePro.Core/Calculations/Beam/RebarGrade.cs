namespace HPDcePro.Core.Calculations.Beam
{
    /// <summary>
    /// Standard reinforcing steel grades per TCVN 5574:2018 Tables 13, 14, 15.
    /// </summary>
    public enum RebarGrade
    {
        CB240T,
        CB300V,
        CB400V,
        CB500V
    }

    /// <summary>
    /// Design material properties for reinforcing steel per TCVN 5574:2018.
    /// </summary>
    public sealed class RebarProperties
    {
        public RebarGrade Grade { get; }
        public string Name => FormatGradeName(Grade);

        /// <summary>Design longitudinal tensile yield strength Rs (MPa).</summary>
        public double Rs { get; }

        /// <summary>Design compressive strength Rsc (MPa) capped at 400 MPa per Clause 8.1.2.3.1.</summary>
        public double Rsc { get; }

        /// <summary>Design transverse stirrup tensile strength Rsw (MPa).</summary>
        public double Rsw { get; }

        /// <summary>Modulus of elasticity Es (MPa), standard 200,000 MPa.</summary>
        public double Es { get; }

        /// <summary>Relative boundary depth of compression zone xi_R = 0.8 / (1 + Rs / 700).</summary>
        public double XiR { get; }

        /// <summary>Boundary resistance coefficient alpha_R = xi_R * (1 - 0.5 * xi_R).</summary>
        public double AlphaR { get; }

        public RebarProperties(RebarGrade grade, double rs, double rsc, double rsw, double es = 200000.0)
        {
            Grade = grade;
            Rs = rs;
            Rsc = rsc;
            Rsw = rsw;
            Es = es;
            XiR = 0.8 / (1.0 + (rs / 700.0));
            AlphaR = XiR * (1.0 - 0.5 * XiR);
        }

        private static string FormatGradeName(RebarGrade grade)
        {
            return grade switch
            {
                RebarGrade.CB240T => "CB240-T",
                RebarGrade.CB300V => "CB300-V",
                RebarGrade.CB400V => "CB400-V",
                RebarGrade.CB500V => "CB500-V",
                _ => grade.ToString()
            };
        }
    }
}
