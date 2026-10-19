using ClosedXML.Excel;
using HPDcePro.Excel;
using Xunit;

namespace HPDcePro.Core.Tests.Excel
{
    public sealed class ClosedXmlUdfEvaluationTests
    {
        [Fact]
        public void ClosedXml_RegisterAll_SetsCustomPropertyAndRegistersUdf()
        {
            using var wb = new XLWorkbook();
            DceUdfFunctions.RegisterAll(wb);

            Assert.Contains(wb.CustomProperties, p => p.Name == "HPDcePro_TCVN_UDF_Registered");
            Assert.True(wb.CustomProperties.CustomProperty("HPDcePro_TCVN_UDF_Registered").GetValue<bool>());
        }

        [Fact]
        public void Udf_ConcreteMechanics_EvaluatesB25AndB30()
        {
            double rbB25 = DceUdfFunctions.TCVN_Rb("B25");
            double rbtB25 = DceUdfFunctions.TCVN_Rbt("B25");
            double ebB25 = DceUdfFunctions.TCVN_Eb("B25");

            Assert.Equal(14.5, rbB25, 1);
            Assert.Equal(1.05, rbtB25, 2);
            Assert.Equal(30000.0, ebB25, 1);

            double rbB30 = DceUdfFunctions.TCVN_Rb("B30");
            Assert.Equal(17.0, rbB30, 1);
        }

        [Fact]
        public void Udf_RebarMechanics_EvaluatesCB400VAndCB240T()
        {
            double rsCB400 = DceUdfFunctions.TCVN_Rs("CB400-V");
            double rsCB240 = DceUdfFunctions.TCVN_Rs("CB240-T");

            Assert.Equal(350.0, rsCB400, 1);
            Assert.Equal(210.0, rsCB240, 1);
        }

        [Fact]
        public void Udf_BeamCalculations_ComputesFlexuralRebarAndCrack()
        {
            double asReq = DceUdfFunctions.TCVN_As_Beam(300.0, 600.0, 150.0, "B25", "CB400-V", 40.0);
            Assert.True(asReq > 500.0 && asReq < 1500.0, $"Expected As in range 500..1500 mm2, got {asReq}");

            double crackWidth = DceUdfFunctions.TCVN_Beam_CrackWidth(300.0, 600.0, 100.0, 1000.0, 20.0, "B25", "CB400-V", 40.0, true);
            Assert.True(crackWidth > 0.0 && crackWidth < 0.5, $"Expected crack width in range 0..0.5 mm, got {crackWidth}");
        }

        [Fact]
        public void Udf_PileCalculations_ComputesBearingCapacityAndFriction()
        {
            double rcSpt = DceUdfFunctions.TCVN_Pile_Rc_SPT(0.4, 20.0, 15.0, 25.0, isDriven: true);
            Assert.True(rcSpt > 300.0, $"Expected pile capacity > 300 kN, got {rcSpt}");

            double skinFriction = DceUdfFunctions.TCVN_Pile_SkinFriction(0.4, 20.0, 15.0, isDriven: true, isCohesive: true);
            Assert.True(skinFriction > 100.0, $"Expected skin friction > 100 kN, got {skinFriction}");
        }

        [Fact]
        public void Udf_SteelCalculations_EvaluatesStabilityAndPMM()
        {
            double beamRatio = DceUdfFunctions.TCVN_Steel_BeamCheck(400.0, 200.0, 8.0, 13.0, 120.0, 6.0, 50.0, 235.0);
            Assert.True(beamRatio > 0.0 && beamRatio < 2.0, $"Expected beam utilization ratio in range 0..2.0, got {beamRatio}");

            double colRatio = DceUdfFunctions.TCVN_Steel_ColumnCheck(350.0, 250.0, 10.0, 16.0, 600.0, 80.0, 3.0, 3.0, 235.0);
            Assert.True(colRatio > 0.0 && colRatio < 2.0, $"Expected column utilization ratio in range 0..2.0, got {colRatio}");
        }

        [Fact]
        public void Udf_WindPressure_EvaluatesZoneII()
        {
            double windPressure = DceUdfFunctions.TCVN_Wind_Pressure("II", "B", 10.0, 0.8, 0.0, true);
            Assert.True(windPressure > 0.5 && windPressure < 2.5, $"Expected wind pressure in range 0.5..2.5 kN/m2, got {windPressure}");
        }

        [Fact]
        public void Udf_EvaluateFunction_DynamicDispatcherEvaluatesAccurately()
        {
            var resRb = DceUdfFunctions.EvaluateFunction("TCVN_RB", "B25");
            Assert.Equal(14.5, (double)resRb!, 1);

            var resRs = DceUdfFunctions.EvaluateFunction("TCVN_RS", "CB400-V");
            Assert.Equal(350.0, (double)resRs!, 1);

            var resAs = DceUdfFunctions.EvaluateFunction("TCVN_AS_BEAM", 300, 600, 150);
            Assert.True((double)resAs! > 0.0);
        }
    }
}
