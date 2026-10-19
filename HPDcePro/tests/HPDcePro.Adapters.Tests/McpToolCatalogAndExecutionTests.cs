using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HPDcePro.Mcp.Catalog;
using HPDcePro.Mcp.Execution;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    public sealed class McpToolCatalogAndExecutionTests
    {
        private readonly DceProMcpToolCatalog _catalog = new();
        private readonly DceProMcpToolExecutor _executor = new();

        [Fact]
        public void ToolCatalog_ContainsExactlyEightToolsWithValidSchemas()
        {
            var tools = _catalog.GetAllTools();
            Assert.Equal(8, tools.Count);

            string[] expectedNames =
            {
                "dce_design_beam", "dce_design_column_wall", "dce_design_foundation_pile",
                "dce_design_slab_stair_lintel", "dce_design_steel_peb", "dce_optimize_rebar_cutting",
                "dce_query_mayhoc_rebar", "dce_run_autostr_pipeline"
            };

            foreach (string name in expectedNames)
            {
                Assert.True(_catalog.HasTool(name), $"Catalog should have {name}");
                var tool = _catalog.GetTool(name);
                Assert.NotNull(tool);
                Assert.False(string.IsNullOrWhiteSpace(tool.Description));
                Assert.NotNull(tool.InputSchema);
                Assert.True(tool.InputSchema is System.Collections.IDictionary dict && dict.Contains("type"));
            }
        }

        [Fact]
        public async Task ExecuteAsync_DceDesignBeam_HandlesOperations()
        {
            var singleSpanArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "single_span",
                sectionWidthMm = 220.0,
                sectionHeightMm = 400.0,
                momentKnm = 85.0,
                shearKn = 95.0
            });
            var singleRes = await _executor.ExecuteAsync("dce_design_beam", singleSpanArgs);
            Assert.True(singleRes.IsSuccess, singleRes.ErrorMessage);

            var continuousArgs = JsonSerializer.SerializeToElement(new { operation = "continuous" });
            var contRes = await _executor.ExecuteAsync("dce_design_beam", continuousArgs);
            Assert.True(contRes.IsSuccess, contRes.ErrorMessage);

            var shopArgs = JsonSerializer.SerializeToElement(new { operation = "shop_drawing" });
            var shopRes = await _executor.ExecuteAsync("dce_design_beam", shopArgs);
            Assert.True(shopRes.IsSuccess, shopRes.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_DceDesignColumnWall_HandlesPmmAndShearWall()
        {
            var pmmArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "column_pmm",
                widthBMm = 300.0,
                heightHMm = 300.0,
                axialNKn = 800.0,
                momentMxKnm = 60.0
            });
            var pmmRes = await _executor.ExecuteAsync("dce_design_column_wall", pmmArgs);
            Assert.True(pmmRes.IsSuccess, pmmRes.ErrorMessage);

            var wallArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "shear_wall",
                lengthLwMm = 2000.0,
                thicknessTwMm = 250.0,
                heightHwMm = 3300.0,
                axialNKn = 1200.0,
                shearVuKn = 450.0,
                momentMuKnm = 900.0
            });
            var wallRes = await _executor.ExecuteAsync("dce_design_column_wall", wallArgs);
            Assert.True(wallRes.IsSuccess, wallRes.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_DceDesignFoundationPile_HandlesPileCapAndFooting()
        {
            var capArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "pile_cap",
                widthBMm = 2200.0,
                lengthLMm = 2200.0,
                thicknessHMm = 900.0,
                axialNKn = 2500.0
            });
            var capRes = await _executor.ExecuteAsync("dce_design_foundation_pile", capArgs);
            Assert.True(capRes.IsSuccess, capRes.ErrorMessage);

            var footingArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "pad_footing",
                widthBMm = 1800.0,
                lengthLMm = 2000.0,
                thicknessHMm = 600.0,
                axialNKn = 650.0
            });
            var footingRes = await _executor.ExecuteAsync("dce_design_foundation_pile", footingArgs);
            Assert.True(footingRes.IsSuccess, footingRes.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_DceDesignSlabStairLintel_HandlesAllOperations()
        {
            var slabArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "two_way_slab",
                shortSpanLxMm = 4000.0,
                longSpanLyMm = 5000.0,
                slabThicknessHMm = 120.0
            });
            var slabRes = await _executor.ExecuteAsync("dce_design_slab_stair_lintel", slabArgs);
            Assert.True(slabRes.IsSuccess, slabRes.ErrorMessage);

            var stairArgs = JsonSerializer.SerializeToElement(new { operation = "staircase" });
            var stairRes = await _executor.ExecuteAsync("dce_design_slab_stair_lintel", stairArgs);
            Assert.True(stairRes.IsSuccess, stairRes.ErrorMessage);

            var lintelArgs = JsonSerializer.SerializeToElement(new { operation = "lintel" });
            var lintelRes = await _executor.ExecuteAsync("dce_design_slab_stair_lintel", lintelArgs);
            Assert.True(lintelRes.IsSuccess, lintelRes.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_DceDesignSteelPeb_HandlesPortalFrameAndConnections()
        {
            var frameArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "portal_frame",
                spanMm = 24000.0,
                eaveHeightMm = 7000.0
            });
            var frameRes = await _executor.ExecuteAsync("dce_design_steel_peb", frameArgs);
            Assert.True(frameRes.IsSuccess, frameRes.ErrorMessage);

            var momentArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "moment_connection",
                bendingMomentKnm = 180.0,
                shearForceKn = 120.0
            });
            var momentRes = await _executor.ExecuteAsync("dce_design_steel_peb", momentArgs);
            Assert.True(momentRes.IsSuccess, momentRes.ErrorMessage);

            var basePlateArgs = JsonSerializer.SerializeToElement(new { operation = "base_plate" });
            var bpRes = await _executor.ExecuteAsync("dce_design_steel_peb", basePlateArgs);
            Assert.True(bpRes.IsSuccess, bpRes.ErrorMessage);

            var windArgs = JsonSerializer.SerializeToElement(new { operation = "wind_load" });
            var windRes = await _executor.ExecuteAsync("dce_design_steel_peb", windArgs);
            Assert.True(windRes.IsSuccess, windRes.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_DceOptimizeRebarCutting_OptimizesAndAggregates()
        {
            var demands = new[]
            {
                new { diameterMm = 18, lengthMm = 4500.0, quantity = 10, mark = "B1-T1" },
                new { diameterMm = 18, lengthMm = 3200.0, quantity = 8, mark = "B1-B1" }
            };

            var cutArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "optimize_cutting",
                stockBarLengthMm = 11700.0,
                demands
            });
            var cutRes = await _executor.ExecuteAsync("dce_optimize_rebar_cutting", cutArgs);
            Assert.True(cutRes.IsSuccess, cutRes.ErrorMessage);

            var aggArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "aggregate_schedule",
                demands
            });
            var aggRes = await _executor.ExecuteAsync("dce_optimize_rebar_cutting", aggArgs);
            Assert.True(aggRes.IsSuccess, aggRes.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_DceQueryMayHocRebar_TokenizesTextAndQueriesModel()
        {
            var tokArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "tokenize_text",
                text = "3d20 + 2d18 L=4200 a150"
            });
            var tokRes = await _executor.ExecuteAsync("dce_query_mayhoc_rebar", tokArgs);
            Assert.True(tokRes.IsSuccess, tokRes.ErrorMessage);

            var modelArgs = JsonSerializer.SerializeToElement(new { operation = "model_info" });
            var modelRes = await _executor.ExecuteAsync("dce_query_mayhoc_rebar", modelArgs);
            Assert.True(modelRes.IsSuccess, modelRes.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_DceRunAutostrPipeline_LoadsConfig()
        {
            var cfgArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "load_config",
                projectName = "Test HPDcePro Pipeline"
            });
            var res = await _executor.ExecuteAsync("dce_run_autostr_pipeline", cfgArgs);
            Assert.True(res.IsSuccess, res.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_DceDesignColumnWall_ColumnDetailing_GeneratesPrimitives()
        {
            var detailArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "column_detailing",
                elementMark = "C1",
                widthBMm = 400.0,
                heightHMm = 400.0
            });
            var res = await _executor.ExecuteAsync("dce_design_column_wall", detailArgs);
            Assert.True(res.IsSuccess, res.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_DceDesignFoundationPile_BoredPileSPT_CalculatesCapacity()
        {
            var boredArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "pile_capacity",
                diameterM = 0.8,
                lengthM = 25.0,
                installationType = "BoredPile",
                tipSoilN60 = 35.0
            });
            var res = await _executor.ExecuteAsync("dce_design_foundation_pile", boredArgs);
            Assert.True(res.IsSuccess, res.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_DceDesignBeam_CurvedBeam_CalculatesTorsion()
        {
            var curvedArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "curved",
                sectionWidthMm = 250.0,
                sectionHeightMm = 500.0,
                momentKnm = 110.0,
                shearKn = 90.0,
                torsionKnm = 25.0,
                radiusMm = 6000.0
            });
            var res = await _executor.ExecuteAsync("dce_design_beam", curvedArgs);
            Assert.True(res.IsSuccess, res.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_UnknownOrInvalidTool_ReturnsFailure()
        {
            var resUnknown = await _executor.ExecuteAsync("dce_unknown_tool", null);
            Assert.False(resUnknown.IsSuccess);
            Assert.Contains("not recognized", resUnknown.ErrorMessage);

            var resEmpty = await _executor.ExecuteAsync(string.Empty, null);
            Assert.False(resEmpty.IsSuccess);
            Assert.Contains("must not be null or empty", resEmpty.ErrorMessage);
        }

        [Fact]
        public async Task ExecuteAsync_InvalidOperation_ReturnsFailure()
        {
            var badArgs = JsonSerializer.SerializeToElement(new { operation = "invalid_operation_name" });
            var res = await _executor.ExecuteAsync("dce_design_beam", badArgs);
            Assert.False(res.IsSuccess);
            Assert.Contains("Unsupported operation", res.ErrorMessage);
        }
    }
}
