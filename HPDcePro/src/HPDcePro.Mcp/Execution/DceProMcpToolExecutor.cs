using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HPDcePro.Mcp.Execution.Handlers;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Execution
{
    public sealed class DceProMcpToolExecutor
    {
        private readonly BeamToolHandler _beamHandler = new();
        private readonly ColumnWallToolHandler _columnWallHandler = new();
        private readonly FoundationToolHandler _foundationHandler = new();
        private readonly SlabSecondaryToolHandler _slabSecondaryHandler = new();
        private readonly SteelPebToolHandler _steelPebHandler = new();
        private readonly RebarCuttingToolHandler _rebarCuttingHandler = new();
        private readonly MayHocToolHandler _mayHocHandler = new();
        private readonly AutoStrToolHandler _autoStrHandler = new();

        public async Task<DceProToolResult> ExecuteAsync(
            string toolName,
            JsonElement? arguments,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toolName))
            {
                return DceProToolResult.Fail("Tool name must not be null or empty.");
            }

            try
            {
                return toolName.ToLowerInvariant() switch
                {
                    "dce_design_beam" => _beamHandler.Execute(arguments),
                    "dce_design_column_wall" => _columnWallHandler.Execute(arguments),
                    "dce_design_foundation_pile" => _foundationHandler.Execute(arguments),
                    "dce_design_slab_stair_lintel" => _slabSecondaryHandler.Execute(arguments),
                    "dce_design_steel_peb" => _steelPebHandler.Execute(arguments),
                    "dce_optimize_rebar_cutting" => _rebarCuttingHandler.Execute(arguments),
                    "dce_query_mayhoc_rebar" => await _mayHocHandler.ExecuteAsync(arguments, cancellationToken).ConfigureAwait(false),
                    "dce_run_autostr_pipeline" => await _autoStrHandler.ExecuteAsync(arguments, cancellationToken).ConfigureAwait(false),
                    _ => DceProToolResult.Fail($"Tool '{toolName}' is not recognized in HPDcePro tool catalog.")
                };
            }
            catch (Exception ex)
            {
                // Reason: Capture unexpected calculation failures into structured MCP failure result
                return DceProToolResult.Fail($"Execution error in '{toolName}': {ex.Message}");
            }
        }
    }
}
