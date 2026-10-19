using System;
using System.Collections.Generic;
using HPDcePro.Mcp.Catalog.Schemas;
using HPDcePro.Mcp.Models;

namespace HPDcePro.Mcp.Catalog
{
    public sealed class DceProMcpToolCatalog
    {
        private readonly Dictionary<string, DceProToolDefinition> _tools = new(StringComparer.OrdinalIgnoreCase);

        public DceProMcpToolCatalog()
        {
            RegisterTool(BeamToolSchema.CreateDefinition());
            RegisterTool(ColumnWallToolSchema.CreateDefinition());
            RegisterTool(FoundationToolSchema.CreateDefinition());
            RegisterTool(SlabSecondaryToolSchema.CreateDefinition());
            RegisterTool(SteelPebToolSchema.CreateDefinition());
            RegisterTool(RebarToolSchema.CreateDefinition());
            RegisterTool(MayHocToolSchema.CreateDefinition());
            RegisterTool(AutoStrToolSchema.CreateDefinition());
        }

        private void RegisterTool(DceProToolDefinition tool)
        {
            _tools[tool.Name] = tool;
        }

        public IReadOnlyList<DceProToolDefinition> GetAllTools()
        {
            return new List<DceProToolDefinition>(_tools.Values);
        }

        public DceProToolDefinition? GetTool(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return _tools.TryGetValue(name, out var tool) ? tool : null;
        }

        public bool HasTool(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && _tools.ContainsKey(name);
        }
    }
}
