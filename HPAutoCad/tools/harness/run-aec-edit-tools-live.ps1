# Unattended live check of the AEC write tools (phase C: create_entities_batch, update_entities_batch, manage_blocks_attributes,
# manage_annotations, manage_hatches, manage_xrefs) in a real AutoCAD 2026 — the same launcher as run-aec-tools-live.ps1 with
# aec-edit-tools-live.py and its own output folder (the xref DWG the run writes lands there too).
#Requires -Version 7.3
param(
    [string]$Exe = (Join-Path $PSScriptRoot '..\..\HPAutoCad.Mcp.Server\bin\Debug\net10.0\HPAutoCad.Mcp.Server.exe'),
    [string]$OutDir = (Join-Path $PSScriptRoot '..\..\output\aec-edit-tools-live'),
    [int]$StartupTimeoutSec = 420
)
& (Join-Path $PSScriptRoot 'run-aec-tools-live.ps1') -Exe $Exe -OutDir $OutDir -Script 'aec-edit-tools-live.py' -StartupTimeoutSec $StartupTimeoutSec
exit $LASTEXITCODE
