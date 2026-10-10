# Search-set rebuild — Analyze (2026-10-10)

Sources: `RuleClash(HP)` (21 groups, mandatory), `SearchSets(HP)` (reference), 6 legacy XML (36 sets, identical in all 6 files), live THCSLT copy (19 files, 86 999 Revit elements, Feet).

## Legacy XML (36 sets) — what is inherited
| Legacy set | Condition | Verdict |
|---|---|---|
| ARC-Furnitures / Ceilings / Columns / Doors / Windows / Walls | Category = X, any file | inherit category; **add file role** (no discipline scope → STR walls leak into ARC-Walls) |
| ARC-CurtainWalls | Category like `Curtain*` | inherit (Curtain Panels, Curtain Wall Mullions, Curtain Systems) |
| ARC-Floors | Floors AND Structural = false | inherit as Structural ≠ true (live: Negate keeps the 20 floors without the parameter; `= false` would drop them) |
| ARC-Stair&Railings | Stairs OR Railings | widen with railing sub-categories found live (Top Rails, Handrails, Supports) + Ramps |
| STR-Walls | Walls AND Structural = true | **not usable**: Structural parameter absent on every wall of both files (live) → file role only |
| STR-Floors / Columns / Framings / Foundations | Category = X | inherit + file role ES |
| ELEC-CableTrays | Trays OR Tray Fittings | → M2 |
| ELEC-ElectricalEquipments | Electrical Equipment OR **Mechanical Equipment** | split (audit F07): Mechanical Equipment → M6 |
| ELEC-Communication(+AV)/Conduits/Data/ElecFixtures/LightingDevices/Security | Category = X | → M5 (legacy EDE tests use exactly these + Electrical Equipment) |
| ELEC-LightingFixtures | Category = X | → M7 (legacy "Lighting Fixtures & Accessories" tests use only this set) |
| FIRE-Pipe / PipeFittings | + System Classification = Fire Protection Wet | → M4 detail (Fire) |
| FIRE-PipeAccessories | Name contains `HPC-KVS-PipeAcc` | **dropped**: project-specific name; System Classification used instead |
| FIRE-ProtectionDevices | Fire Protection | → M4 (legacy fire tests put it with fire pipes) |
| FIRE-AlarmDevices | Fire Alarm Devices | → M5 (electrical device; legacy mixed it into fire-pipe tests) — **BIM lead to confirm** |
| PLB-Pipe / PipeFittings | Classification ≠ Fire Protection Wet | replaced by per-system detail sets |
| PLB-Drainage*/Supply* | Pipes AND Classification = X | → M4 detail sets, fittings added; `Other` = rain water confirmed live by System Type `TNM` |
| PLB-PlungbingFixtures | Plumbing Fixtures | auxiliary set (not a matrix group) |

## Live facts that decide conditions
| Fact | Value |
|---|---|
| discipline key | `Item > Source File` = Revit local copy `…-<ROLE>-0001_<user>.rvt` → wildcard `*-<ROLE>-????*` |
| Structural (`lcldrevit_parameter_-1001954`) | walls: absent in AA and ES; floors AA 204 false / 49 true / 20 absent; ES 114 true / 54 absent |
| Mechanical Equipment | 48, all in EE (wall AC units, local heaters), classification Power → M6 by category |
| Pipes | 5 972; Diameter (`-1140225`) ≥ 32 mm: 4 493; < 32 mm: 1 479 |
| Pipe systems (Classification / System Type) | Fire Protection Wet: CC TRONG NHÀ, CC NGOÀI NHÀ; Domestic Cold Water: CNL, CN-L; Sanitary: SH, TP, TN(BM), TNT; Vent: TH (+TP fittings); Other: TNM |
| Cable trays | EC 41 (ELV), EE 92, EF 1 |
| Not in any matrix group (by category) | ARC: Plumbing Fixtures 927, Generic Models 638, Sprinklers 198, Structural Columns 132, Structural Connections 118, Structural Framing 29, Wall Sweeps 23, Roofs 12, Rooms 138; STR: Generic Models 26 604, Plates 660, Bolts 240, Curtain Wall Mullions 93, Anchors 30; MEP: Plumbing Fixtures 381, Plumbing Equipment 7, Center Line ~10 500 |

## API behaviour verified live
- `SearchConditionCollection.AddGroup` = OR between groups, AND inside.
- `SearchCondition.Negate()` on `Structural = true` matches items without the parameter (224 = 204 + 20).
- `CompareWith(NumericGreaterThanOrEqual, FromDoubleLength(units.ToDrawing(32)))` on Diameter: 4 493.

## Decisions needing the BIM lead (defaults applied, flagged in the registry)
1. Fire Alarm Devices → M5 (legacy grouped with fire pipes).
2. Fire Protection, Sprinklers (MEP files) → M4.
3. ARC floors with Structural = true (49) excluded from A6 (legacy rule); reported.
4. Elements of matrix categories modelled in the "wrong" discipline file (ARC Structural Columns/Framing, ARC Plumbing Fixtures/Sprinklers, STR Curtain Wall Mullions) stay out of base sets; reported, plus auxiliary sets so they can be seen.
5. Methodology note 3 (no pipes D < 32 mm) → detail set `M4 Clash scope (D ≥ 32 mm)`; base M4 stays complete.
