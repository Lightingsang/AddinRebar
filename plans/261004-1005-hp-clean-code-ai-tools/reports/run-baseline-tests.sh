#!/usr/bin/env bash
# Throw-away: run baseline test projects sequentially, one log per project.
ROOT="/f/1-CONG VIEC/05-AI/01_Revit/02_Csharp/AddinRebar"
LOGS="$ROOT/plans/261004-1005-hp-clean-code-ai-tools/reports/test-logs"
SUMMARY="$LOGS/summary.tsv"
: > "$SUMMARY"
run() {
  local dir="$1" proj="$2"
  local start end rc
  start=$(date +%s)
  ( cd "$ROOT/$dir" && dotnet test "$proj" ) > "$LOGS/$proj.log" 2>&1
  rc=$?
  end=$(date +%s)
  printf '%s\t%s\t%s\t%s\n' "$dir" "$proj" "$rc" "$((end-start))" >> "$SUMMARY"
}
run McpShared HPRebar.Mcp.Server.Core.Tests
run McpShared HPRebar.McpBridge.Core.Net48Tests
for h in HPRebar HPAutoCad HPCivil3d HPNavis HPEtabs HPSap2000 HPRobot HPExcel HPPowerBi HPTekla; do
  run "$h" "$h.Mcp.Server.Tests"
done
echo DONE >> "$SUMMARY"
