# HP Search Set Registry — THCSLT copy, 2026-10-10

Source of truth: `HPNavis/HPNavis.BIMCoordinator/SearchSets/hp-base-sets.json` (schema 2, embedded in the engine; reusable config). Every set = OR over (Element > Category = C AND Item > Source File like `*-<role>-????*` AND extra conditions); category tested first. Counts = Navisworks resolving the saved set on `THCSLT-HPC-ZZ-ZZ-CM-ZZ-0001-searchsets.nwd` (copy of the original, SHA256 B27F…D667).

## 21 base sets (RuleClash(HP) groups)

| Code | Folder | Roles | Categories | Extra conditions | THCSLT | Legacy XML on same model | Evidence |
|---|---|---|---|---|---:|---|---|
| A1 Furniture | HP BIMCoordinator/Architecture | AA | Furniture, Furniture Systems, Casework | — | 0 | ARC-Furnitures 0 | legacy ARC-Furnitures (Furniture); Furniture Systems and Casework are the other Revit furniture categories (none in THCSLT) |
| A2 Ceilings | HP BIMCoordinator/Architecture | AA | Ceilings | — | 142 | ARC-Ceilings 142 | legacy ARC-Ceilings |
| A3 Architectural Columns | HP BIMCoordinator/Architecture | AA | Columns | — | 0 | ARC-Columns 0 | legacy ARC-Columns |
| A4 Curtain Walls | HP BIMCoordinator/Architecture | AA | Curtain Panels, Curtain Wall Mullions, Curtain Systems | — | 13,635 | ARC-CurtainWalls 13 728 (93 from ES) | legacy ARC-CurtainWalls (Category like Curtain*) |
| A5 Doors | HP BIMCoordinator/Architecture | AA | Doors | — | 275 | ARC-Doors 275 | legacy ARC-Doors |
| A6 Architectural Floors | HP BIMCoordinator/Architecture | AA | Floors | Structural <> true (or absent) | 224 | ARC-Floors 208 (4 from EP; 20 unflagged lost) | legacy ARC-Floors (Structural = false); live: 20 ARC floors lack the parameter, so the condition is Structural <> true |
| A7 Stairs and Railings | HP BIMCoordinator/Architecture | AA | Stairs, Railings, Top Rails, Handrails, Supports, Ramps | — | 979 | ARC-Stair&Railings 370 | legacy ARC-Stair&Railings (Stairs OR Railings); live railing sub-categories Top Rails 244, Handrails 25, Supports 338, Ramps 2 |
| A8 Architectural Walls | HP BIMCoordinator/Architecture | AA | Walls | — | 8,502 | ARC-Walls 8 623 (121 from ES) | legacy ARC-Walls; Structural absent on every wall (live), so ARC/STR walls are split by source file only |
| A9 Windows | HP BIMCoordinator/Architecture | AA | Windows | — | 339 | ARC-Windows 339 | legacy ARC-Windows |
| S1 Foundations | HP BIMCoordinator/Structure | ES | Structural Foundations | — | 388 | STR-Foundations 388 | legacy STR-Foundations |
| S2 Structural Walls | HP BIMCoordinator/Structure | ES | Walls | — | 121 | STR-Walls 0 (Structural absent) | legacy STR-Walls (Structural = true) — parameter absent on all walls (live), so source file ES only |
| S3 Structural Columns | HP BIMCoordinator/Structure | ES | Structural Columns | — | 1,580 | STR-Columns 1 712 (132 from AA) | legacy STR-Columns |
| S4 Structural Floors | HP BIMCoordinator/Structure | ES | Floors | — | 168 | STR-Floors 445 (273 from AA, 4 EP) | legacy STR-Floors; live ES floors: 114 Structural = true, 54 without the parameter — all kept |
| S5 Structural Framing | HP BIMCoordinator/Structure | ES | Structural Framing | — | 6,069 | STR-Framings 6 098 (29 from AA) | legacy STR-Framings |
| M1 Air Terminals | HP BIMCoordinator/MEP | EC, EE, EF, EP | Air Terminals | — | 0 | — (missing) | missing in legacy (audit F01); Revit category Air Terminals |
| M2 Cable Ladders and Trays | HP BIMCoordinator/MEP | EC, EE, EF, EP | Cable Trays, Cable Tray Fittings | — | 134 | ELEC-CableTrays 134 | legacy ELEC-CableTrays |
| M3 Ducts and Duct Accessories | HP BIMCoordinator/MEP | EC, EE, EF, EP | Ducts, Duct Fittings, Duct Accessories | — | 0 | — (missing) | missing in legacy (audit F01); flex ducts excluded (RuleClash methodology note 3) |
| M4 Pipes and Pipe Accessories | HP BIMCoordinator/MEP | EC, EE, EF, EP | Pipes, Pipe Fittings, Pipe Accessories, Fire Protection, Sprinklers | — | 10,744 | FIRE-* + PLB-Pipe/PipeFittings (no accessories outside fire) | legacy FIRE-Pipe/PipeFittings/PipeAccessories/ProtectionDevices + PLB pipes/fittings (legacy fire and plumbing tests); flex pipes excluded (methodology note 3); Sprinklers added as fire-pipe accessories — BIM lead to confirm |
| M5 Electrical Distribution and Accessories | HP BIMCoordinator/MEP | EC, EE, EF, EP | Electrical Equipment, Electrical Fixtures, Communication Devices, Audio Visual Devices, Data Devices, Security Devices, Lighting Devices, Fire Alarm Devices, Conduits, Conduit Fittings | — | 1,089 | ELEC EDE sets 991 incl. 48 Mechanical Equipment | legacy EDE tests (ELEC Communication+AV, Conduit*, Data, ElectricalEquipments without Mechanical Equipment, ElectricalFixtures, LightingDevices, SecurityDevices); Fire Alarm Devices moved from the fire group — BIM lead to confirm |
| M6 HVAC Equipment | HP BIMCoordinator/MEP | EC, EE, EF, EP | Mechanical Equipment | — | 48 | — (inside ELEC-ElectricalEquipments) | split out of legacy ELEC-ElectricalEquipments (audit F07); live: 48 wall AC units / heaters in the EE file |
| M7 Lighting Fixtures and Accessories | HP BIMCoordinator/MEP | EC, EE, EF, EP | Lighting Fixtures | — | 1,677 | ELEC-LightingFixtures 1 717 (40 from AA) | legacy ELEC-LightingFixtures (the only set of the legacy Lighting Fixtures & Accessories tests) |

## Detail sets

| Code | Folder | Parent | Roles | Categories | Conditions | THCSLT | Evidence |
|---|---|---|---|---|---|---:|---|
| M2.1 Cable trays - EC model | HP BIMCoordinator/MEP/Details/M2 Cable trays by model | M2 | EC | Cable Trays, Cable Tray Fittings | — | 41 | live: 41 trays/fittings in the EC (ELV) file |
| M2.2 Cable trays - EE model | HP BIMCoordinator/MEP/Details/M2 Cable trays by model | M2 | EE | Cable Trays, Cable Tray Fittings | — | 92 | live: 92 trays/fittings in the EE (power) file |
| M2.3 Cable trays - EF model | HP BIMCoordinator/MEP/Details/M2 Cable trays by model | M2 | EF | Cable Trays, Cable Tray Fittings | — | 1 | live: 1 tray in the EF file |
| M4.1 Fire Protection Wet | HP BIMCoordinator/MEP/Details/M4 Pipes by system | M4 | EC, EE, EF, EP | Pipes, Pipe Fittings, Pipe Accessories | System Classification = Fire Protection Wet | 480 | legacy FIRE-Pipe/PipeFittings; live systems CC TRONG NHÀ, CC NGOÀI NHÀ |
| M4.2 Domestic Cold Water | HP BIMCoordinator/MEP/Details/M4 Pipes by system | M4 | EC, EE, EF, EP | Pipes, Pipe Fittings, Pipe Accessories | System Classification = Domestic Cold Water | 4,057 | legacy PLB-SupplyColdWater; live systems CNL, CN-L |
| M4.3 Domestic Hot Water | HP BIMCoordinator/MEP/Details/M4 Pipes by system | M4 | EC, EE, EF, EP | Pipes, Pipe Fittings, Pipe Accessories | System Classification = Domestic Hot Water | 0 | legacy PLB-SupplyHotWater (none in THCSLT) |
| M4.4 Sanitary | HP BIMCoordinator/MEP/Details/M4 Pipes by system | M4 | EC, EE, EF, EP | Pipes, Pipe Fittings, Pipe Accessories | System Classification = Sanitary | 4,054 | legacy PLB-DrainageSanitary; live systems SH, TP, TN(BM), TNT |
| M4.5 Vent | HP BIMCoordinator/MEP/Details/M4 Pipes by system | M4 | EC, EE, EF, EP | Pipes, Pipe Fittings, Pipe Accessories | System Classification = Vent | 399 | legacy PLB-DrainageSanitaryVent; live system TH |
| M4.6 Other (rain water) | HP BIMCoordinator/MEP/Details/M4 Pipes by system | M4 | EC, EE, EF, EP | Pipes, Pipe Fittings, Pipe Accessories | System Classification = Other | 1,669 | legacy PLB-DrainageRainWater (Other); live: every Other pipe has System Type TNM |
| M4.7 Fire devices and sprinklers | HP BIMCoordinator/MEP/Details/M4 Pipes by system | M4 | EC, EE, EF, EP | Fire Protection, Sprinklers | — | 73 | legacy FIRE-ProtectionDevices; Sprinklers per SearchSets(HP) Fire list |
| M4.9 Clash scope - pipes D >= 32 mm | HP BIMCoordinator/MEP/Details/M4 Clash scope | M4 | EC, EE, EF, EP | Pipes + (no condition) Pipe Fittings, Pipe Accessories, Fire Protection, Sprinklers | Diameter >= 32 mm | 9,265 | RuleClash(HP) methodology note 3: no clash for pipes D < 32 mm (live: 1 479 of 5 972 pipes); fittings and accessories are not filtered by size |

## Auxiliary sets (not in the matrix — visibility only)

| Code | Folder | Parent | Roles | Categories | Conditions | THCSLT | Evidence |
|---|---|---|---|---|---|---:|---|
| X.1 MEP plumbing fixtures and equipment | HP BIMCoordinator/Not in matrix | — | EC, EE, EF, EP | Plumbing Fixtures, Plumbing Equipment | — | 388 | legacy PLB-PlungbingFixtures; no RuleClash(HP) group |
| X.2 ARC plumbing fixtures and sprinklers | HP BIMCoordinator/Not in matrix | — | AA | Plumbing Fixtures, Plumbing Equipment, Sprinklers | — | 1,130 | live: modelled in the ARC file (927 + 5 + 198) |
| X.3 ARC structural elements | HP BIMCoordinator/Not in matrix | — | AA | Structural Columns, Structural Framing, Structural Connections, Structural Foundations | — | 279 | live: structural categories in the ARC file (132 + 29 + 118) — outside S1..S5, which take the ES file only |
| X.4 ARC floors flagged structural | HP BIMCoordinator/Not in matrix | — | AA | Floors | Structural = true | 49 | excluded from A6 by the legacy rule (live: 49) |
| X.5 STR curtain wall mullions | HP BIMCoordinator/Not in matrix | — | ES | Curtain Wall Mullions, Curtain Panels | — | 93 | live: 93 mullions in the ES file — outside A4, which takes the ARC file only |
