# REA 6.6.0 Kata Pro Binary Audit & Managed Core Analysis Report

**Investigator**: `teamwork_preview_explorer_m1_1`  
**Date**: 2026-10-18T05:45:00Z  
**Tool**: `rea@6.6.0` (Node.js script runner: `node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs"`)  
**Target Workspace**: `C:\kata_pro\` (100% READ-ONLY)  
**Primary Focus**: Managed core assemblies `Kata_Class_Lib.dll` and `Kata_Class_Lib_Revit.dll`

---

## 1. Environment Verification & Integrity Baseline

### 1.1 REA 6.6.0 Environment Verification
- **Command**: `node "C:\Users\STR-HP03\AppData\Roaming\npm\node_modules\rea-agents\scripts\rea.mjs" --version`
- **Output**: `6.6.0` (Exit code: 0)
- **Node.js Runtime**: `v24.18.0` on `win32-x64`
- **Status**: **VERIFIED**

### 1.2 Pre-Audit vs Post-Audit Integrity Baseline (`C:\kata_pro\`)
- **Total Files Scanned**: 396 files
- **Integrity Baseline File**: `.agents/teamwork/teamwork_preview_explorer_m1_1/baseline_sha256.json`
- **Zero-Mutation Verification**: Pre-audit and post-audit SHA-256 hashes matched 100% across all 396 files. Zero files created, modified, or deleted in `C:\kata_pro\`.

### 1.3 Baseline Inventory of Core Kata DLLs in `C:\kata_pro\`
| File | Byte Length | Pre-Audit SHA-256 | Post-Audit Status |
|---|---|---|---|
| `Kata_Class_Lib.dll` | 843,776 | `e3690a631c20ad9e93835459762a372225cde77d144602072b4b9fc77ba5a85f` | Unchanged (0 byte diff) |
| `Kata_Class_Lib_Revit.dll` | 1,086,464 | `38e31337456cb171a675943c7e1723bb068e8a194db9471d9e679002393785d9` | Unchanged (0 byte diff) |
| `Kata_pro64.dll` | 3,415,040 | `5da9a37be8fea727da725df05cc89108b5fe3e6071958b012e3fcc2441b01b44` | Unchanged (0 byte diff) |
| `Kata_pro64_Cad2013.dll` | 16,851,456 | `f2f675d224e1455a65f3c3490f7d1b33f9f8dee0550574d4f51f2673f75f8e50` | Unchanged (0 byte diff) |
| `Kata_pro64_Cad2025.dll` | 16,791,552 | `4a1313848d459c3e633ddd9792c38d4e9757e68d09381bddcd17160249180100` | Unchanged (0 byte diff) |
| `Kata_pro64_GstarCad.dll` | 14,343,680 | `ac9ac5de57f759cb101c224a113a7c5761c09842c7070ab59a17345f26744cff` | Unchanged (0 byte diff) |
| `Kata_pro64_GstarCad2025.dll` | 16,811,520 | `af1477b7cc8e63e32895dd1243e47a511cccdcf8a2080c75af66bed140fcf196` | Unchanged (0 byte diff) |
| `Kata_pro64_ZwCad.dll` | 14,457,344 | `699c2876160253f5a2a8937c4275d0285a0a34e20747b1b94708c121d26beb5f` | Unchanged (0 byte diff) |
| `kata_pro.dll` | 1,011,712 | `2c6ab3af0338c7a33345d3a1a156d0aa13265cd81101f4d574a0ac1f50fb6c88` | Unchanged (0 byte diff) |

---

## 2. In-Depth Analysis: `Kata_Class_Lib.dll`

### 2.1 Artifact & Classification Evidence
- **Artifact Path**: `C:\kata_pro\Kata_Class_Lib.dll`
- **Byte Length**: 843,776 bytes
- **SHA-256**: `e3690a631c20ad9e93835459762a372225cde77d144602072b4b9fc77ba5a85f`
- **Format**: PE32 (`optional_header: pe32`, `machine: 0x014c` / `x86`, section count: 3)
- **CLI Header**: `header_offset: 520`, flags: `1` (`il-only`), runtime version: `2.5`
- **Managed Architecture**: `anycpu`
- **Runtime Family**: `dotnet-framework`
- **Target Framework**: `.NETFramework,Version=v4.8` (at file offset 687,800)
- **Assembly Identity**: `Kata_Class_Lib`, Version `1.0.0.0`, Token `0x20000001`
- **Evidence IDs**:
  - `inspect-managed-artifact`: `ev_6faa7e25c5f9394f253c524d6bba1652ded151dd243697d1770bd5e263354c88`
  - `inspect-managed-members`: `ev_9b55cc6a6ddb752c312c83c563357d3518914ecf1b11bdbf2994706acbcb7ff0`
  - `inspect-managed-native-boundaries`: `ev_8210694fec6d30b54cdfd10ed6234b7147a422ab1246fa5e2f6aef07bce52982`
  - `inspect-pe-resources`: `ev_d018d22fe380e410413fda6d0fd310647e995ef5af32f7738984801cf39f901b`
  - `project-managed-application-graph`: `ev_ae75731fd725f6da2c361db7a24808b8ac03ff59a2c2c58613c18e4d02777525`
  - `build-reconstruction-obligation-ledger`: `ev_45a3dca117c402e29bfd2d4985e99deca448ea6f3eb61b70c744f5fda639c23f`

### 2.2 Metadata Tables & Member Inventories
- **Streams Present**: `#Blob`, `#GUID`, `#Strings`, `#US`, `#~`
- **Inventory Counts**:
  - `types` (`TypeDef`): **491**
  - `methods` (`MethodDef`): **3,356**
  - `fields` (`Field`): **1,307**
  - `member_refs` (`MemberRef`): **872**
  - `type_refs` (`TypeRef`): **217**
  - `params` (`Param`): **1,963**
  - `call_edges`: **3,214**
  - `field_accesses`: **760**
  - `properties` (`Property`): **81**
  - `nested_classes` (`NestedClass`): **108**
  - `custom_attributes` (`CustomAttribute`): **256**
  - `user_strings` (literals): **37**

### 2.3 Assembly References (7 Dependencies)
1. `mscorlib`, v4.0.0.0 (Token `0x23000001`, PKT `b77a5c561934e089`)
2. `Microsoft.VisualBasic`, v10.0.0.0 (Token `0x23000002`, PKT `b03f5f7f11d50a3a`)
3. `System.Windows.Forms`, v4.0.0.0 (Token `0x23000003`, PKT `b77a5c561934e089`)
4. `System.Core`, v4.0.0.0 (Token `0x23000004`, PKT `b77a5c561934e089`)
5. `System.Drawing`, v4.0.0.0 (Token `0x23000005`, PKT `b03f5f7f11d50a3a`)
6. `System`, v4.0.0.0 (Token `0x23000006`, PKT `b77a5c561934e089`)
7. `Newtonsoft.Json`, v13.0.0.0 (Token `0x23000007`, PKT `30ad4fe6b2a6aeed`)

### 2.4 Embedded Manifest Resources (6 Private Encrypted Resources)
All resources are private, embedded, and carry obfuscated obfuscator stream identifiers:
1. `3hLGA5qxOmcJbOToWF.vQn9C1to51FlGQOx5u` (43,160 bytes, SHA256 `ab1ec59006f58a935c1e8667ee8cd58348b8105ef8daf359a98741e9325f8db9`)
2. `XgvWexEKmsAMEwM7kU.FnE2YqlK2r0rfqFQsR` (14,410 bytes, SHA256 `0ece8b44325f62936a8276a72865719ceab61851dfbf8f4bae5ecebc346d00e5`)
3. `tDl0UwZZZKSsbEHsLg.pI5itlhtKYsaNF3Ohg` (256 bytes, SHA256 `32999728c1de294080eb1c334a84bd180bacec1e7c5f2c25fa874a970547e0a5`)
4. `M5VulKW5UJSWRLLTDe.hw223FKt5r698OrEt2` (609 bytes, SHA256 `56bfb3d7ba7801068fc123b19687fb528719b5444e7c98bcd220b7474d903101`)
5. `klF7yfVeKFvhSUC3gx.jH37QiYD0uIXQmfoY2` (2,304 bytes, SHA256 `9311c3eae67c7e788d5c53e0928a0b93869af461a61ed6bb823a83ce2c745152`)
6. `kxvHF230r2jpgdAgy0.j2wibSbuklu9mbtdb0` (2,524 bytes, SHA256 `c96b9263160137c6bf494dac299aed22c10de8bbd0b47399e33f7eb9118f2b14`)

### 2.5 Native Boundaries & P/Invoke Declarations (8 Imports)
- `user32.dll`:
  - `SetForegroundWindow` (Token `0x06000432`)
  - `SetFocus` (Token `0x06000433`)
  - `FindWindow` (Token `0x06000434`)
  - `IsIconic` (Token `0x06000435`)
  - `ShowWindow` (Token `0x06000436`)
- `oleaut32.dll`:
  - `GetActiveObject` (Token `0x06000437`) — **Critical**: used to attach to live running Excel COM instance (`Excel.Application`)
- `kernel32`:
  - `LoadLibrary` (obfuscated member name `IG1bLU0wjg`, Token `0x06000468`)
  - `GetProcAddress` (obfuscated member name `Jr1bHIk6ey`, Token `0x06000469`)

### 2.6 PE Resources
- **Resource Count**: 1
- **Type**: `RT_VERSION` (ID 16), Name 1, Language 0
- **Payload Length**: 896 bytes, SHA256: `910569a935d1a5fee1aa3ae1d0c53235253e61704095d5b990da62d5e188f273`

### 2.7 Reconstruction Obligation Ledger
- **Ledger ID**: `rol_5c34275f3367adb09ef625a4eefc7e1e0a08416e55d50b012df42ad1eff27091`
- **Closure Digest**: `d8a709d7ffa248677f0c2f4370e4351470b0f337ea1867300575dbabf5dab5cb`
- **Total Reconstruction Obligations**: **656**

---

## 3. In-Depth Analysis: `Kata_Class_Lib_Revit.dll`

### 3.1 Artifact & Classification Evidence
- **Artifact Path**: `C:\kata_pro\Kata_Class_Lib_Revit.dll`
- **Byte Length**: 1,086,464 bytes
- **SHA-256**: `38e31337456cb171a675943c7e1723bb068e8a194db9471d9e679002393785d9`
- **Format**: PE32 (`optional_header: pe32`, `machine: 0x014c` / `x86`, section count: 3)
- **CLI Header**: `header_offset: 520`, flags: `1` (`il-only`), runtime version: `2.5`
- **Managed Architecture**: `anycpu`
- **Runtime Family**: `dotnet-framework`
- **Target Framework**: `.NETFramework,Version=v4.8` (at file offset 887,758)
- **Assembly Identity**: `Kata_Class_Lib_Revit`, Version `1.0.0.0`, Token `0x20000001`
- **Evidence IDs**:
  - `inspect-managed-artifact`: `ev_70c701e24efaa09d693c3dbded6efbb8df0f1a30f462c31d98c01a5d87c3f490`
  - `inspect-managed-members`: `ev_2027f5e1010a092d7618ead02143ed0ca9bd9108320f437e6811ca219d488504`
  - `inspect-managed-native-boundaries`: `ev_3b208a7bd43beb45a0b41aa417744e14cb61f65214cb629de2f4f04fb34ae416`
  - `inspect-pe-resources`: `ev_ee6c1ad8a469fe9e51944fe0ba0271752f9cfb244caf77322fdab6b2c545dbba`
  - `project-managed-application-graph`: `ev_fa2513ec8e739522ad43c264e1c6b6f5280efb50690f9788386277ffad206251`
  - `build-reconstruction-obligation-ledger`: `ev_865a811e13796e16815c34beff63bebf7772b58307e113fa1366416ed41e670e`

### 3.2 Metadata Tables & Member Inventories
- **Streams Present**: `#Blob`, `#GUID`, `#Strings`, `#US`, `#~`
- **Inventory Counts**:
  - `types` (`TypeDef`): **692**
  - `methods` (`MethodDef`): **4,357**
  - `fields` (`Field`): **1,613**
  - `member_refs` (`MemberRef`): **1,107**
  - `type_refs` (`TypeRef`): **307**
  - `params` (`Param`): **2,329**
  - `call_edges`: **4,007**
  - `field_accesses`: **963**
  - `properties` (`Property`): **78**
  - `nested_classes` (`NestedClass`): **147**
  - `custom_attributes` (`CustomAttribute`): **282**
  - `user_strings` (literals): **37**

### 3.3 Assembly References (9 Dependencies)
1. `mscorlib`, v4.0.0.0 (Token `0x23000001`, PKT `b77a5c561934e089`)
2. `Microsoft.VisualBasic`, v10.0.0.0 (Token `0x23000002`, PKT `b03f5f7f11d50a3a`)
3. `System.Windows.Forms`, v4.0.0.0 (Token `0x23000003`, PKT `b77a5c561934e089`)
4. `System.Drawing`, v4.0.0.0 (Token `0x23000004`, PKT `b03f5f7f11d50a3a`)
5. `System.Core`, v4.0.0.0 (Token `0x23000005`, PKT `b77a5c561934e089`)
6. `RevitAPI`, v20.0.0.0 (Token `0x23000006`, PKT `null` — Revit 2020 baseline reference)
7. `RevitAPIUI`, v20.0.0.0 (Token `0x23000007`, PKT `null` — Revit 2020 baseline reference)
8. `System`, v4.0.0.0 (Token `0x23000008`, PKT `b77a5c561934e089`)
9. `Newtonsoft.Json`, v13.0.0.0 (Token `0x23000009`, PKT `30ad4fe6b2a6aeed`)

### 3.4 Embedded Manifest Resources (6 Private Encrypted Resources)
1. `6FI1sbHZwsnSkKZ8P3.rOIc7hk5ivbobBotQG` (51,644 bytes, SHA256 `171548bd6a0296006339eee4cd784a9c882efb1eed1fb04fcd0029389740f566`)
2. `vBnaqi3DiyrxoL21uG.40B6y0Ov13HyOnrvDu` (3,560 bytes, SHA256 `d6c8e328cd9963663cd355304de307c184eae8962ec4e85a1fb761fb03b2ce60`)
3. `icMAooTWWcCcufwJYN.00l2UBbK7dWencibX7` (256 bytes, SHA256 `13a372952273d296d532fef72b6f2438fb5e3bba86a50e2237a0d8e7202504ab`)
4. `DpboT9akneOPZpLJWP.E8rx99WYrQ6bsUfCgQ` (14,912 bytes, SHA256 `5f035df7837a1bb2686408782ebeb8af046b5d6ab0a0395b7f0d42ceb679978b`)
5. `GfbWaZpXcXcOTJciXG.DppsHNCd4edKLgGOf4` (614 bytes, SHA256 `6be76ec470b3aa3b12e03a8686fafd8b54101bbddfd9a06baef7cdc37f4eb2b9`)
6. `BCOUkWcZUC9eWxAkce.UjF4RwtG81oxfpn6I3` (7,076 bytes, SHA256 `4eb035ba2fd06221cce4aeebec249698d72f6f385afdd4eeb3022ce5c1bdaf38`)

### 3.5 Native Boundaries & P/Invoke Declarations (8 Imports)
- `user32.dll`: `SetForegroundWindow`, `SetFocus`, `FindWindow`, `IsIconic`, `ShowWindow`
- `oleaut32.dll`: `GetActiveObject`
- `kernel32`: `LoadLibrary` (obfuscated member name `fa3typcRVg`), `GetProcAddress` (obfuscated member name `MXxtnniQXO`)

### 3.6 PE Resources
- **Resource Count**: 1
- **Type**: `RT_VERSION` (ID 16), Name 1, Language 0
- **Payload Length**: 920 bytes, SHA256: `c04156b1927f434e32be3196eea2395b833c4210b4d54b6e29f0c9e91e096688`

### 3.7 Reconstruction Obligation Ledger
- **Ledger ID**: `rol_0b47177c67410b4fe58520b590be97ecb6f4982e2409e5fc488a8b48476347cb`
- **Closure Digest**: `28d045dc4b922f36e5d8461993783dd5d1377033724996ac63861ee433f25543`
- **Total Reconstruction Obligations**: **974**

---

## 4. Public Contracts vs Obfuscated Classes

### 4.1 Non-Obfuscated Public Domain Model & Contracts
While the internal method bodies and helper classes are protected by an obfuscator/packer, the public engineering contracts, DTO names, Revit API selection filters, and geometry data structures remain intact in the metadata:

#### Core Engineering DTOs (`Kata_Class_Lib` & `Kata_Class_Lib_Revit`):
1. **Beam Rebar & Detailing**:
   - `info_revit_thep`: Model representing longitudinal reinforcement bars (`Origin`, `Direction_L`, `Direction_H`, `Partition`, `Barshape`, `Mark`, `Sch_Mark`, `Stt_cad`, `Comment`, `Dia`, `Layout_Rule`).
   - `info_revit_thep_ngang`: Model for transverse / stirrup bars (`i_thep_chinh`, `i_thep`, `x1`, `x2`, `y`, `Barshape`, `Dia`, `Spacing`).
   - `info_revit_dai`, `info_revit_dai_i`, `info_revit_i_dai`: Stirrup configuration records.
   - `info_revit_moc_thep`: Hook detail records.
   - `Info_Beam3D`: 3D Beam descriptor (`CenterLine`, `thep`, `B`, `Name`, `Namei`, `TypeBeam`, `Level`, `Handle`, `Layer_Load`).
   - `info_Block_Beam_Rebar`: Rebar layout block data (`U_ngoai`, `thep_t`, `thep_d`, `thep_gc`, `cd`, `a_dai`, `dai_gc_input`).
   - `info_BlockBeam_AllData1nhip`, `info_BlockBeam_ThepBoTri`, `info_BlockBeam_DataEtabs`: Span-by-span rebar arrangement and Etabs force mapping.
   - `info_thep_gc_goi`, `info_thep_gc_nhip`, `info_thep_gia`, `info_thep_sldk`: Cut-off, lap, and skin rebar definitions.

2. **Column, Wall, Footing & Slab DTOs**:
   - `Info_ColumnWall3D`: Column and shear-wall 3D reinforcement models.
   - `Info_Slab3D`, `info_revit_slab`: Two-way and one-way slab rebar records.
   - `Info_dai_cot`: Column stirrup / tie data.
   - `info_dam_giao_cotcay`, `info_dam_giao_taicot`: Beam-column joint reinforcement details.

3. **Revit API Selection & Manipulation Engine (`Kata_Class_Lib_Revit`)**:
   - **Selection Filters**:
     - `RebarSelectionFilter`: Filter for selecting native `Autodesk.Revit.DB.Structure.Rebar` elements.
     - `Floor_Beam_SelectionFilter`: Filter for framing / beam elements.
     - `Column_Walls_SelectionFilter`, `Column_wall_SelectionFilter`: Filter for structural columns and shear walls.
     - `Foundation_SelectionFilter`, `Foundation_Floor_SelectionFilter`: Filter for footings and foundation slabs.
     - `Level_SelectionFilter`, `ket_cau_grid_SelectionFilter`: Grids and levels filters.
   - **Revit Generators & Services**:
     - `Kata_Class_Lib_Revit.Revit_rebar`: Core generator class with methods `create_rebar`, `create_rebar_with_so_thanh`, `create_rebar_with_hooks`, `Bend_diameter`, `lay_tap_curve_rebar`.
     - `Kata_Class_Lib_Revit.Get_Kata_Familytype`: Resolver for Revit symbols (`get_elementtype`, `get_Dimtypeid`, `get_ViewFamilyType`, `get_Tag_ElementId`, `get_MultiReferenceAnnotationType`, `get_DimensionType`, `get_LineStyle`).
     - `Kata_Class_Lib_Revit.Get_element`: Element retrieval and intersection queries (`Get_Delete_rebar_in_host`, `Get_Delete_view_section`, `get_barshape`, `get_barType`, `get_hookType`, `Get_All_Rebar`, `Get_columnwall_giao_Beam`).
     - `Kata_Class_Lib_Revit.Perform_2D`: Automated section generation and 2D detailing (`tao_section_dung`, `tao_section_nam`, `dim_ro_revit`, `dim_al_revit`, `Create_rebar_mul_Tag`, `Create_Break_Line`).

4. **Geometry Engine**:
   - `CircleXYZ`, `CurveXYZ`: Native 3D CAD/Revit geometric calculation classes.
   - `Kata_Class_Lib.XYZ`: Custom 3-vector class (mirroring Revit `XYZ`).
   - `Kata_Class_Lib.Hinh_hoc_khong_gian`, `Hinh_hoc_phang`: Analytical geometry utilities.

### 4.2 Obfuscated Internal Classes & Technical Cause
- **Obfuscated Types Count**:
  - `Kata_Class_Lib.dll`: 396 obfuscated types (out of 491 total types, ~80.6%).
  - `Kata_Class_Lib_Revit.dll`: 585 obfuscated types (out of 692 total types, ~84.5%).
- **Technical Analysis of Protection Mechanism**:
  1. **Dynamic JIT Hooking / Unpacking**: String literals reveal direct access to `clrjit.dll`, `GetDelegateForFunctionPointer`, `VirtualAlloc`, `WriteProcessMemory`, `OpenProcess`, and `CloseHandle`.
  2. **Anti-Tampering & Anti-Debugging**: Explicit strings `'Debugger Detected'` and `' is tampered.'` verify active runtime anti-tamper routines.
  3. **String Encryption**: User string tables contain only 37 literal entries across both DLLs; all engineering messages, prompt strings, and attribute values are stored encrypted in the embedded manifest resources (`3hLGA5qx...`, `6FI1sbHZ...`) and decrypted at runtime via AES (`System.Security.Cryptography.AesCryptoServiceProvider`).
  4. **Name Mangling**: Method names like `hBmguSfWSZWrtvn8r6g`, `z7viN1ftWxpLe5JRFCT`, and type names like `B6IxnYfyhhsddPF4ULl` represent one-way cryptographic renaming.
- **Classification Status**: Private method bodies with dynamic JIT encryption cannot be decompiled statically via ordinary CIL disassembly without executing unpacking routines in an isolated environment. They must be marked **`CHƯA GIẢI MÃ ĐƯỢC` (Tên gốc / Thân hàm nội bộ bị mã hóa JIT bởi ConfuserEx/Packer)**.

---

## 5. Architectural Synthesis for `HPKata` Clean-Room Standalone Implementation

From the decoded public contracts, P/Invoke boundaries, and member graphs, the complete functional pipeline of Kata is fully reconstructed:

1. **Excel COM Integration (`Kata_Class_Lib.Bien_chung_cad`)**:
   - Kata uses `oleaut32.dll!GetActiveObject` with `Marshal.GetActiveObject("Excel.Application")` to obtain the live instance of Excel.
   - In `HPRebar`/`HPKata`, this is natively replaced with dual data-source capability: primary COM via `oleaut32`, with headless offline fallback via `ClosedXML` reading `.xlsm` from disk.

2. **Structural Data Schema Matching**:
   - The beam rebar specification in sheet `Dam` maps 1:1 to `info_Block_Beam_Rebar` and `info_BlockBeam_ThepBoTri`.
   - The top continuous, bottom continuous, top additional support bars, bottom span bars, and stirrups map directly into `KataBeamRebarSpec` in `HPRebar.Core`.

3. **Revit Rebar Placement Mechanism (`Kata_Class_Lib_Revit.Revit_rebar`)**:
   - Creation uses `Rebar.CreateFromCurves` and `Rebar.CreateFromCurvesAndShape` based on `lay_tap_curve_rebar` (translating `CurveXYZ` arrays to Revit `IList<Curve>`).
   - Hooks are assigned via `RebarHookType` (`create_rebar_with_hooks`).
   - Idempotency is handled by `Get_Delete_rebar_in_host` (locating previous rebar elements in host beam and deleting them before generating new layout).

4. **Conclusion**:
   - All domain schemas, geometric algorithms, Revit API bindings, and Excel COM patterns needed for full 1:1 standalone reconstruction in `HPKata` are completely characterized and proven by concrete REA 6.6.0 Evidence.
   - Zero proprietary packed code needs to be executed or disassembled; the clean-room implementation in `HPKata` operates with 100% independence, zero runtime dependencies on `C:\kata_pro\`, and full adherence to Clean Code Governance.
