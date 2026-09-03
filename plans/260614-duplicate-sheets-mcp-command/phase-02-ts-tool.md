# Phase 02 — TS tool `duplicate_sheets.ts`

**Priority:** P0 · **Status:** ☐ · **Depends on:** none (verified with Phase 01 in Phase 03)

## Overview

The TypeScript half: a Zod-validated MCP tool that relays params over TCP to the C# `duplicate_sheets` command.
`register.ts` auto-discovers `tools/*.ts` exporting a `register*` function → **no register edit, no command.json**.

## Files

- **Create:** `mcp/server/src/tools/duplicate_sheets.ts`

## duplicate_sheets.ts (sketch — mirrors create_grid.ts)

```ts
import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerDuplicateSheetsTool(server: McpServer) {
  server.tool(
    "duplicate_sheets",
    "Batch-duplicate existing Revit sheets by sheet number — no UI. For each source sheet, creates a new sheet " +
      "with a transformed number/name and copies its title block, viewports (per mode), legends, schedules and " +
      "sheet annotations. find/replace/prefix/suffix apply to BOTH number and name; sequence numbering applies to " +
      "the sheet number only. The whole batch runs in one atomic transaction (a single undo) — pass all sheets at " +
      "once; no per-call limit.",
    {
      sourceSheetNumbers: z.array(z.string()).min(1)
        .describe("Sheet numbers (exactly as shown in Revit) of the sheets to duplicate."),
      findText: z.string().optional().describe("Substring to find in number AND name (omit to skip)."),
      replaceText: z.string().optional().describe("Replacement for findText (empty string removes findText)."),
      prefix: z.string().optional().describe("Prepended to number AND name."),
      suffix: z.string().optional().describe("Appended to number AND name."),
      useSequence: z.boolean().default(false).describe("Append an incrementing number to the sheet NUMBER."),
      sequenceStart: z.number().int().default(1).describe("First sequence value (when useSequence=true)."),
      sequencePadding: z.number().int().default(0).describe("Zero-pad the sequence to N digits (0 = none)."),
      mode: z.enum(["JustSheet", "Duplicate", "WithDetailing", "AsDependent"]).default("JustSheet")
        .describe("View duplication mode for non-legend views. JustSheet and Duplicate are aliases = plain copy."),
      copyTitleBlock: z.boolean().default(true).describe("Align the title block to match the source position."),
      includeLegends: z.boolean().default(true).describe("Place the same legend views on the new sheet."),
      includeSchedules: z.boolean().default(true).describe("Re-place schedule instances."),
      copySheetAnnotations: z.boolean().default(true)
        .describe("Copy sheet-level text/dims/detail lines/filled regions/revision clouds."),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((revit) => revit.sendCommand("duplicate_sheets", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text",
          text: `Duplicate sheets failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
```

> `args` keys are camelCase and match `DuplicateSheetsParameters` `[JsonProperty(...)]` 1:1 → pass straight through.
> Confirm `withRevitConnection` call shape against a sibling tool (e.g. `create_grid.ts`) before finalizing.

## Todo

- [ ] Create `duplicate_sheets.ts`
- [ ] `cd mcp/server && npm run build` → emits `build/tools/duplicate_sheets.js`, no TS errors
- [ ] Server-start log shows `已注册工具: duplicate_sheets.js`

## Success criteria

- TS tool name == C# `CommandName` == `duplicate_sheets`. Zod fields == args == `[JsonProperty]` names.

## Risks

- **Stale build dir:** registered server runs `mcp/server/build/index.js` → must `npm run build`.
- **Enum drift:** keep TS `mode` enum in sync with C# `MapMode`.

## Open questions

- None. (Mode exposes both `JustSheet` + `Duplicate`, default `JustSheet`.)
