using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using HPDcePro.Mcp;
using HPDcePro.Mcp.Execution;
using Xunit;

namespace HPDcePro.Adapters.Tests
{
    public sealed class ChallengerMcpStressTests
    {
        private readonly DceProMcpToolExecutor _executor = new();
        private readonly DceProMcpServer _server = new();

        [Fact]
        public async Task McpExecutor_ExtremeGeometry_ReturnsStructuredResult_NoCrash()
        {
            var zeroBeam = JsonSerializer.SerializeToElement(new
            {
                operation = "single_span",
                sectionWidthMm = -220.0,
                sectionHeightMm = 0.0,
                momentKnm = 50.0
            });
            var resBeam = await _executor.ExecuteAsync("dce_design_beam", zeroBeam);
            Assert.NotNull(resBeam);

            var negWall = JsonSerializer.SerializeToElement(new
            {
                operation = "shear_wall",
                lengthLwMm = -100.0,
                thicknessTwMm = -50.0,
                heightHwMm = 0.0
            });
            var resWall = await _executor.ExecuteAsync("dce_design_column_wall", negWall);
            Assert.NotNull(resWall);

            var zeroFooting = JsonSerializer.SerializeToElement(new
            {
                operation = "pad_footing",
                widthBMm = 0.0,
                lengthLMm = -500.0,
                thicknessHMm = 0.0
            });
            var resFooting = await _executor.ExecuteAsync("dce_design_foundation_pile", zeroFooting);
            Assert.NotNull(resFooting);
            Assert.False(resFooting.IsSuccess);
        }

        [Fact]
        public async Task McpExecutor_EnormousForces_10Power7_ReturnsStructuredResult_NoCrash()
        {
            double massiveForce = 1e7;
            var massiveCol = JsonSerializer.SerializeToElement(new
            {
                operation = "column_pmm",
                widthBMm = 400.0,
                heightHMm = 400.0,
                axialNKn = massiveForce,
                momentMxKnm = massiveForce,
                momentMyKnm = massiveForce
            });
            var resCol = await _executor.ExecuteAsync("dce_design_column_wall", massiveCol);
            Assert.NotNull(resCol);

            var massivePeb = JsonSerializer.SerializeToElement(new
            {
                operation = "moment_connection",
                bendingMomentKnm = massiveForce,
                shearForceKn = massiveForce
            });
            var resPeb = await _executor.ExecuteAsync("dce_design_steel_peb", massivePeb);
            Assert.NotNull(resPeb);

            var massivePileCap = JsonSerializer.SerializeToElement(new
            {
                operation = "pile_cap",
                axialNKn = massiveForce,
                momentMxKnm = massiveForce
            });
            var resPileCap = await _executor.ExecuteAsync("dce_design_foundation_pile", massivePileCap);
            Assert.NotNull(resPileCap);
        }

        [Fact]
        public async Task McpExecutor_NullAndMalformedArguments_ReturnsGracefully()
        {
            var resNull = await _executor.ExecuteAsync("dce_design_beam", null);
            Assert.NotNull(resNull);

            var resEmpty = await _executor.ExecuteAsync("dce_design_beam", JsonSerializer.SerializeToElement(new { }));
            Assert.NotNull(resEmpty);

            var arrayArg = JsonSerializer.SerializeToElement(new[] { "arg1", "arg2" });
            var resArray = await _executor.ExecuteAsync("dce_design_column_wall", arrayArg);
            Assert.NotNull(resArray);

            var stringArg = JsonSerializer.SerializeToElement("scalar_string");
            var resString = await _executor.ExecuteAsync("dce_design_foundation_pile", stringArg);
            Assert.NotNull(resString);
        }

        [Fact]
        public async Task McpExecutor_UnknownToolAndInvalidOperations_ReturnsStructuredFailure()
        {
            var resUnknownTool = await _executor.ExecuteAsync("dce_non_existent_tool_xyz", null);
            Assert.False(resUnknownTool.IsSuccess);
            Assert.Contains("not recognized", resUnknownTool.ErrorMessage);

            string[] tools =
            {
                "dce_design_beam", "dce_design_column_wall", "dce_design_foundation_pile",
                "dce_design_slab_stair_lintel", "dce_design_steel_peb"
            };

            var badOp = JsonSerializer.SerializeToElement(new { operation = "totally_unknown_operation" });
            foreach (var tool in tools)
            {
                var res = await _executor.ExecuteAsync(tool, badOp);
                Assert.False(res.IsSuccess);
                Assert.Contains("Unsupported operation", res.ErrorMessage);
            }
        }

        [Fact]
        public async Task McpExecutor_RebarAndMayHoc_ExtremeDemandsAndMalformedText()
        {
            var zeroStock = JsonSerializer.SerializeToElement(new
            {
                operation = "optimize_cutting",
                stockBarLengthMm = 0.0,
                demands = new[] { new { diameterMm = 18, lengthMm = 3000.0, quantity = 5 } }
            });
            var resCut = await _executor.ExecuteAsync("dce_optimize_rebar_cutting", zeroStock);
            Assert.NotNull(resCut);

            string giantText = new string('A', 50000) + " 3d20 a150";
            var tokArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "tokenize_text",
                text = giantText
            });
            var resTok = await _executor.ExecuteAsync("dce_query_mayhoc_rebar", tokArgs);
            Assert.NotNull(resTok);
            Assert.True(resTok.IsSuccess);

            var gibberishArgs = JsonSerializer.SerializeToElement(new
            {
                operation = "tokenize_text",
                text = "!@#$%^&*()_+~`|}{[]:;?><,./"
            });
            var resGibberish = await _executor.ExecuteAsync("dce_query_mayhoc_rebar", gibberishArgs);
            Assert.NotNull(resGibberish);
            Assert.True(resGibberish.IsSuccess);
        }

        [Fact]
        public async Task McpExecutor_AutoStr_InvalidPaths_ReturnsStructuredFailure()
        {
            var badCfg = JsonSerializer.SerializeToElement(new
            {
                operation = "load_config",
                configPath = "non_existent_folder_xyz/missing_file.autostr"
            });
            var res = await _executor.ExecuteAsync("dce_run_autostr_pipeline", badCfg);
            Assert.NotNull(res);
            Assert.False(res.IsSuccess);
        }

        [Fact]
        public async Task McpExecutor_ConcurrentRapidDispatch_Handles100TasksInParallel()
        {
            var tasks = new List<Task<HPDcePro.Mcp.Models.DceProToolResult>>();
            var beamArg = JsonSerializer.SerializeToElement(new { operation = "single_span", momentKnm = 50.0 });
            var colArg = JsonSerializer.SerializeToElement(new { operation = "column_pmm", axialNKn = 500.0 });
            var windArg = JsonSerializer.SerializeToElement(new { operation = "wind_load", heightZM = 15.0 });

            for (int i = 0; i < 100; i++)
            {
                int rem = i % 3;
                if (rem == 0) tasks.Add(_executor.ExecuteAsync("dce_design_beam", beamArg));
                else if (rem == 1) tasks.Add(_executor.ExecuteAsync("dce_design_column_wall", colArg));
                else tasks.Add(_executor.ExecuteAsync("dce_design_steel_peb", windArg));
            }

            var results = await Task.WhenAll(tasks);
            Assert.Equal(100, results.Length);
            foreach (var r in results)
            {
                Assert.NotNull(r);
                Assert.True(r.IsSuccess, r.ErrorMessage);
            }
        }

        [Fact]
        public async Task McpServer_ProcessMessageAsync_MalformedJsonRpc_ReturnsParseError_NoCrash()
        {
            string malformedJson = "{ invalid json without closing quote";
            string? response = await _server.ProcessMessageAsync(malformedJson);
            Assert.NotNull(response);
            Assert.Contains("-32700", response);
            Assert.Contains("Parse error", response);

            string missingMethod = "{\"jsonrpc\":\"2.0\",\"id\":1}";
            string? resMethod = await _server.ProcessMessageAsync(missingMethod);
            Assert.NotNull(resMethod);
            Assert.Contains("-32601", resMethod);

            string badToolCall = "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/call\",\"params\":{}}";
            string? resTool = await _server.ProcessMessageAsync(badToolCall);
            Assert.NotNull(resTool);
            Assert.Contains("-32602", resTool);
        }
    }
}
