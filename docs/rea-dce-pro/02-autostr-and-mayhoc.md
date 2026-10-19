# Technical Specification: AutoStr 24-Step Structural Workflow & MayHoc ML NLP Engine

**Deliverable Target**: `HPDcePro.Core.Serialization`, `HPDcePro.Core.MachineLearning`  
**Decompiled Baseline**: `C:\Dce Pro\AutoStr\DuAnMau.autostr`, `C:\Dce Pro\MayHoc\mauhoc.db`, `C:\Dce Pro\DCE Pro.dll` (`ClassLibrary1.AutoStr`, `ClassLibrary1.Dce_MayHoc`)  
**Standard Compliance**: TCVN 2737:2023 (Loads & Actions), TCVN 9386:2012 (Earthquake Resistance), TCVN 5574:2018 (Concrete)

---

## 1. AutoStr 24-Step Automated Structural Engineering Workflow

### 1.1 Architecture & Pipeline Design
`AutoStr` represents the core automation orchestration engine of the DCE Pro ecosystem. It transforms 2D architectural CAD plans into a completely designed, modeled, analyzed, detailed, scheduled, and printed structural drawing package.

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    AUTOSTR 24-STEP ENGINEERING PIPELINE                     │
│                                                                             │
│  PHASE 1: Framing & Topology (Steps 01 - 06)                                │
│  Step01_TaoSheetSet ──> Step02_LuoiTruc ──> Step03_DinhViCot                │
│  ──> Step04_MBKC ──> Step05_DuongPhanTichLuc ──> Step06_TaiTuong            │
│                                                                             │
│  PHASE 2: ETABS Modeling & Superstructure Design (Steps 07 - 12)            │
│  Step07_BuildEtabsModel ──> Step08_TinhToanDamExcel ──> Step09_VeChiTietDam │
│  ──> Step10_TinhToanCotExcel ──> Step11_VeChiTietCotVach ──> Step12_VeSan   │
│                                                                             │
│  PHASE 3: Substructure Foundation & Piles (Steps 13 - 19)                   │
│  Step13_MBMongCoc ──> Step14_DPTLMong ──> Step15_EtabsMong                  │
│  ──> Step16_TTMong ──> Step17_VeMong ──> Step18_TTGiangM ──> Step19_VeGiangM│
│                                                                             │
│  PHASE 4: Secondary Elements, Publishing & Reporting (Steps 20 - 24)        │
│  Step20_CKPhu ──> Step21_DanhMuc ──> Step22_MoSheetSet                      │
│  ──> Step23_InAn ──> Step24_TMTT (Calculation Report)                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.2 Exhaustive Step-by-Step Specification (Steps 01 to 24)

| Step # | Class Name | Function Title | Inputs & Dependencies | Processing & Algorithms | Output Artifacts & CAD Layers |
|:---:|---|---|---|---|---|
| **01** | `AutoStrStep01_TaoSheetSet` | Initialize Project & SheetSet | Project root path, sheet set template | Creates directory tree structure (`01.SheetSet` .. `12.CauKienPhu`) and initializes AutoCAD Sheet Set (`.dst`). | Directory tree, `DuAn.dst` |
| **02** | `AutoStrStep02_LuoiTruc` | Structural Grid Extraction | Architectural plan, layer `KT_TRUC` | Reads architectural grid lines, identifies coordinate axes X1-Xn, YA-Yn, re-aligns bubbles, converts to layer `DCE_Truc`. | `DCE_Truc`, `DCE_DIM_Truc` |
| **02b**| `AutoStrStep02b_ExrefCot` | Architectural Column Xref | Multi-story architectural DWGs | Binds or references architectural columns across levels to ensure vertical alignment of structural framing. | `02.Xref\Xref_Cot.dwg` |
| **03** | `AutoStrStep03_DinhViCot` | Column Sizing & Centering | Layer `KT_COT`, Xref columns | Identifies column rectangular/circular boundaries, computes centroids, generates column marks (`C1`, `C2`...), sets up initial sections ($b \times h$). | `DCE_SUB-COWA_----`, `MBDV_Cot.dwg` |
| **04** | `AutoStrStep04_MBKC` | Structural Framing Plan (MBKC) | Column nodes, architectural walls/slabs | Generates primary and secondary beam networks connecting column nodes; identifies slab panel boundaries, bathroom depressions (`KT_SAN_HA_COT`), and slab openings (`KT_LO_MO`). | `DCE_Beam`, `DCE_BaoBeTong`, `DCE_Gen_Open`, `07.MatBangKetCau\MBKC_*.dwg` |
| **05** | `AutoStrStep05_DuongPhanTichLuc` | Tributary Load Lines (Wireframe) | Centerlines of beams and columns | Converts beam 2D envelopes into 1D centerline wireframe model with rigid end offsets at column nodes for FEA analysis. | `03.DuongPhanTichLuc\DPTL_*.dwg` |
| **06** | `AutoStrStep06_TaiTuong` | Wall Line Load Calculation | Layer `KT_TUONG`, wall height $H_{story} - h_{beam}$ | Scans wall thickness ($110\text{ mm}$, $220\text{ mm}$), brick density ($1500\text{ kg/m}^3$), plaster ($1800\text{ kg/m}^3$), computes distributed load $q_{wall} = \gamma \cdot t \cdot H_{net}$ along beams. | Distributed line loads on framing members (kN/m) |
| **07** | `AutoStrStep07_BuildEtabsModel` | ETABS 3D Model Generation | Centerline wireframe, material grades, loads | Connects via CSI ETABS OAPI (`SapModel`), defines materials (`B20`, `CB400-V`), frame sections, floor diaphragms, assigns 14 ULS combos, 14 SLS combos, runs finite-element solver. | `04.Etabs\DuAn.edb` |
| **08** | `AutoStrStep08_TinhToanDamExcel` | Beam Internal Forces Export | ETABS analysis results, combos | Extracts maximum bending moments ($M_{max}^+, M_{max}^-$) and shear ($V_{max}$) per span; exports to `09.DCE_Beam_2020.xlsm`. | `05.ExcelTinhToan\09.DCE_Beam.xlsm` |
| **09** | `AutoStrStep09_VeChiTietDam` | Automated Beam Shop Detailing | Sized beams from Excel, grid offsets | Generates longitudinal elevations, continuous top/bottom bars, support cutoffs ($L/4$), localized dense stirrup zones, section cuts 1-1, 2-2. | `10.ChiTietDam\Dam_Tang_*.dwg` |
| **10** | `AutoStrStep10_TinhToanCotExcel` | Column Internal Forces Export | ETABS column forces ($N, M_x, M_y$) | Exports axial loads and biaxial moments for all stories to `08.DCE_ColumnWall_2020.xlsm`; solves P-M-M interaction under TCVN 5574:2018. | `05.ExcelTinhToan\08.DCE_ColumnWall.xlsm` |
| **11** | `AutoStrStep11_VeChiTietCotVach` | Column & Wall Detailing | Optimized column reinforcement | Produces column schedule tables, cross-sections, multi-story splice locations, and confinement tie detailing. | `09.ChiTietCotVach\Cot_Tang_*.dwg` |
| **12a**| `AutoStrStep12a_TinhToanSan` | Slab Moment Calculation | Floor panel geometry, boundary conditions | Calculates two-way slab bending moments ($M_x, M_y$) based on Wood-Armer yield line theory or FEA strip forces. | Slab design moments (kNm/m) |
| **12** | `AutoStrStep12_VeChiTietSan` | Automated Slab Rebar Detailing | Slab design moments, panel bounds | Places bottom rebar mesh ($\phi 10a200$), top negative moment rebar over supports ($L_0/4$), inserts chair spacer bars, and generates callouts. | `11.ChiTietSan\San_Tang_*.dwg`, `DCE_San_Duoi`, `DCE_San_Tren` |
| **13** | `AutoStrStep13_MBMongCoc` | Foundation & Pile Cap Layout | Column base reactions from ETABS | Positions pile caps (`ĐC.1`..`ĐC.n`) centered on column axes; arranges pile clusters based on pile design capacity $P_{allow}$. | `06.MatBangMong\MB_Mong.dwg` |
| **14** | `AutoStrStep14_DPTLMong` | Foundation Load Tributary Lines | Footing layout, soil reactions | Computes tributary foundation distribution lines and foundation strip meshes for SAFE / ETABS substructure analysis. | Foundation strip geometry |
| **15** | `AutoStrStep15_EtabsMong` | Substructure Soil-Spring Model | Foundation wireframe, pile stiffness | Builds substructure model with vertical and lateral springs ($K_z = 20,000\text{ kN/m}$, $K_x = K_y = 2,000\text{ kN/m}$) at each pile node; solves pile reactions. | `04.Etabs\DuAn_Mong.edb` |
| **16** | `AutoStrStep16_TTMong` | Pile Cap & Punching Calculation | Pile axial loads, column loads | Checks punching shear of column through cap and corner pile through cap; designs pile cap bottom rebar mesh in `13.DCE_DaiCoc_2020.xlsm`. | `05.ExcelTinhToan\13.DCE_DaiCoc.xlsm` |
| **17** | `AutoStrStep17_VeMong` | Pile Cap Detailing Drafting | Designed pile caps and schedules | Drafts plan and section views for all pile cap types, including starter dowels for columns and tie beam embeds. | `06.MatBangMong\ChiTiet_DaiCoc.dwg` |
| **18** | `AutoStrStep18_TTGiangM` | Tie Beam (Giằng Móng) Design | Footing differential settlements | Sizes foundation tie beams (giằng móng) to resist column base moments and minimum axial tension/compression ($N \ge 0.1 N_{column}$). | Tie beam calculation sheet |
| **19** | `AutoStrStep19_VeGiangM` | Tie Beam Detailing Drafting | Tie beam geometry and reinforcement | Drafts longitudinal elevation and cross-sections of foundation tie beams (GM1, GM2...). | `06.MatBangMong\ChiTiet_GiangMong.dwg` |
| **20** | `AutoStrStep20_CKPhu` | Secondary Elements Detailing | Floor elevations, stair parameters | Generates structural detailing for 2-flight / 3-flight staircases, door/window lintels, roof parapets, and elevator pits. | `12.CauKienPhu\ChiTiet_Thang_Lanhto.dwg` |
| **21** | `AutoStrStep21_DanhMuc` | Drawing List & Rebar BOQ | Title blocks across all project DWGs | Scans title block attributes (`FileBlockTenBanVe`), compiles consolidated drawing index sheet and project-wide rebar bill of quantities. | Drawing index sheet, project BOQ |
| **22** | `AutoStrStep22_MoSheetSet` | SheetSet Synchronization | Project sheet set `DuAn.dst` | Registers all generated layouts into AutoCAD Sheet Set Manager; synchronizes project title and sheet numbers. | Synchronized `DuAn.dst` |
| **23** | `AutoStrStep23_InAn` | Automated Batch PDF Publishing | All layouts in SheetSet | Batch plots entire sheet set to PDF using `DWG To PDF.pc3`, monochrome.ctb, ISO full bleed A3. | `InAn\DuAn_Full_BanVe.pdf` |
| **24** | `AutoStrStep24_TMTT` | Structural Calculation Report | Excel sheets, ETABS outputs | Compiles a formatted Microsoft Word `.docx` structural calculation report (Thuyết minh tính toán) with geometry, loads, and check results. | `ThuyetMinhTinhToan.docx` |

### 1.3 Technical Implementation Note: UTF-8 Byte Order Mark (BOM) Handling

Forensic byte-level inspection of the baseline project file `C:\Dce Pro\AutoStr\DuAnMau.autostr` reveals a 3-byte leading UTF-8 Byte Order Mark (BOM: `0xEF, 0xBB, 0xBF`):
- **Parser Exception Risk**: Standard `System.Text.Json.JsonSerializer.Deserialize<T>()` operating on raw bytes or unconfigured streams fails immediately at character index 0 with `JsonException: '0xEF' is an invalid start of a property name`.
- **Specification for `HPDcePro.Core.Serialization.AutoStrProjectSerializer`**:
  - The clean-room deserializer must explicitly account for the UTF-8 BOM preamble.
  - When deserializing from file or byte array buffers:
    ```csharp
    ReadOnlySpan<byte> utf8Bytes = File.ReadAllBytes(filePath);
    if (utf8Bytes.Length >= 3 && utf8Bytes[0] == 0xEF && utf8Bytes[1] == 0xBB && utf8Bytes[2] == 0xBF)
    {
        utf8Bytes = utf8Bytes.Slice(3);
    }
    return JsonSerializer.Deserialize<AutoStrConfig>(utf8Bytes, SerializerOptions);
    ```
  - Alternatively, use a `StreamReader` configured with `new UTF8Encoding(detectEncodingFromByteOrderMarks: true)`.
  - When writing `.autostr` files, preserve UTF-8 compatibility to maintain seamless round-trip interoperability with both `HPDcePro` and legacy CAD commands.

---

## 2. Complete `.autostr` JSON Schema & Serialization Model

The `.autostr` project format is a UTF-8 JSON serialization of `ClassLibrary1.AutoStr.AutoStrConfig`. Below is the complete schema derived from `C:\Dce Pro\AutoStr\DuAnMau.autostr`:

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "AutoStrConfig",
  "type": "object",
  "required": [
    "TenDuAn",
    "ProjectRootPath",
    "MacBtCotVach",
    "MacBtDamSan",
    "MacThep",
    "BeRongDamDienHinh",
    "ChieuCaoDamDienHinh",
    "ChieuDaySanDienHinh",
    "Floors"
  ],
  "properties": {
    "TenDuAn": { "type": "string", "description": "Project title" },
    "ProjectRootPath": { "type": "string", "description": "Root directory path for all project assets" },
    "FileGhiChuChung": { "type": "string", "description": "Absolute path to general notes DWG" },
    "FileKhungTen": { "type": "string", "description": "Absolute path to sheet border / title block DWG" },
    "FileBlockTenBanVe": { "type": "string", "description": "Block name used for drawing titles" },
    "FileChiTietCoc": { "type": "string", "description": "Path to standard pile detail DWG" },
    "ExcelGocPath": { "type": "string", "description": "Root path to reference calculation spreadsheets" },
    "MacBtCotVach": { "type": "string", "enum": ["B15", "B20", "B25", "B30", "B35", "B40"] },
    "MacBtDamSan": { "type": "string", "enum": ["B15", "B20", "B25", "B30", "B35", "B40"] },
    "MacThep": { "type": "string", "enum": ["CB240-T", "CB300-V", "CB400-V", "CB500-V"] },
    "BeRongDamDienHinh": { "type": "number", "default": 220.0 },
    "ChieuCaoDamDienHinh": { "type": "number", "default": 400.0 },
    "BeRongDam2": { "type": "number", "default": 220.0 },
    "ChieuCaoDam2": { "type": "number", "default": 400.0 },
    "BeRongDam3": { "type": "number", "default": 220.0 },
    "ChieuCaoDam3": { "type": "number", "default": 300.0 },
    "ChieuDaySanDienHinh": { "type": "number", "default": 120.0 },
    "ChieuDaySanWC": { "type": "number", "default": 100.0 },
    "GopChiTietDamGiongNhau": { "type": "boolean", "default": false },
    "TienToCot": { "type": "string", "default": "C" },
    "GopCotGiongNhau": { "type": "boolean", "default": true },
    "CotChoTangTietDien": { "type": "boolean", "default": true },
    "CotHeSoTangToiDa": { "type": "number", "default": 2.0 },
    "CotBuocTang": { "type": "number", "default": 50.0 },
    "CocTuTangKhiKhongDat": { "type": "boolean", "default": true },
    "SanSapXepTagThep": { "type": "boolean", "default": true },
    "MbkcKyHieuSan": { "type": "boolean", "default": true },
    "QuyDinhThepMong": { "type": "boolean", "default": false },
    "ThepMongTrenX": { "type": "string", "default": "14a100" },
    "ThepMongTrenY": { "type": "string", "default": "14a100" },
    "ThepMongDuoiX": { "type": "string", "default": "14a100" },
    "ThepMongDuoiY": { "type": "string", "default": "14a100" },
    "MongDayBTLot": { "type": "number", "default": 100.0 },
    "MongBTLotMoRong": { "type": "number", "default": 50.0 },
    "MongBTBVTren": { "type": "number", "default": 35.0 },
    "MongBTBVDuoi": { "type": "number", "default": 135.0 },
    "MongBTBVThanh": { "type": "number", "default": 50.0 },
    "MongNgamCoc": { "type": "number", "default": 100.0 },
    "PrefixTenDai": { "type": "string", "default": "ĐC." },
    "KhoiLuongRiengGach": { "type": "number", "default": 1500.0 },
    "KhoiLuongRiengVua": { "type": "number", "default": 1800.0 },
    "ChieuDayTrat": { "type": "number", "default": 30.0 },
    "SaiSoBatDiem": { "type": "number", "default": 200.0 },
    "TyLeMBKC": { "type": "string", "default": "1:75" },
    "NetIn": { "type": "string", "default": "monochrome.ctb" },
    "MayIn": { "type": "string", "default": "DWG To PDF.pc3" },
    "KhoGiay": { "type": "string", "default": "ISO full bleed A3 (297.00 x 420.00 MM)" },
    "LuonGhiDe": { "type": "boolean", "default": true },
    "TachFileEtabsMong": { "type": "boolean", "default": true },
    "TuPhucHoiUngDung": { "type": "boolean", "default": true },
    "AiNghiemThuMoiBuoc": { "type": "boolean", "default": true },
    "AiNghiemThuCacBuoc": { "type": "string", "default": "4,5,6,7,13,14,15,20" },
    "Layers": {
      "type": "object",
      "properties": {
        "LayerLuoiTruc": { "type": "string", "default": "KT_TRUC" },
        "LayerDimTruc": { "type": "string", "default": "KT_DIM_TRUC" },
        "LayerCot": { "type": "string", "default": "KT_COT" },
        "LayerDam": { "type": "string", "default": "DAM" },
        "LayerBaoSan": { "type": "string", "default": "KT_BAO_SAN" },
        "LayerLoMo": { "type": "string", "default": "KT_LO_MO" },
        "LayerNhaVeSinh": { "type": "string", "default": "KT_SAN_HA_COT" },
        "LayerTuong": { "type": "string", "default": "KT_TUONG" },
        "LayerKcTruc": { "type": "string", "default": "DCE_Truc" },
        "LayerKcCot": { "type": "string", "default": "DCE_SUB-COWA_----" },
        "LayerKcDam": { "type": "string", "default": "DCE_Beam" },
        "LayerKcBaoSan": { "type": "string", "default": "DCE_BaoBeTong" },
        "LayerKcLoMo": { "type": "string", "default": "DCE_Gen_Open" },
        "LayerThepDuoiSuot": { "type": "string", "default": "DCE_San_Duoi" },
        "LayerThepTrenSuot": { "type": "string", "default": "DCE_San_Tren" }
      }
    },
    "Floors": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "StoryIndex": { "type": "integer" },
          "TenTang": { "type": "string" },
          "KyHieuTang": { "type": "string" },
          "FilePathKienTruc": { "type": "string" },
          "ChieuCaoTang": { "type": "number" },
          "CaoDoTang": { "type": "number" },
          "BeRongDam": { "type": "number" },
          "ChieuCaoDam": { "type": "number" },
          "ChieuDaySan": { "type": "number" },
          "HoatTai": { "type": "number" },
          "TinhTai": { "type": "number" }
        }
      }
    }
  }
}
```

---

## 3. MayHoc: NLP Rebar Tokenizer & Machine Learning Engine

### 3.1 Domain Problem Formulation
On Vietnamese construction drawings, structural rebar annotations follow diverse, non-standardized CAD text notations. Examples encountered on real production drawings:
- `[1b] 3x∅20, L= 11700`
- `[1a+3G]6x∅20,L=11700`
- `2d18 + 1d20 L=3500`
- `[3] 4Ф16 a150 L=2800`

Rule-based regular expressions often break down when confronted with nested subscripts, combined mark groups (`1a+3G`), multiple diameter callouts, or irregular spacing. DCE Pro solves this using a **token-level multi-class classification model** powered by `Microsoft.ML`.

### 3.2 Database Schema: `C:\Dce Pro\MayHoc\mauhoc.db`
The SQLite database stores 45,020 labeled training tokens:

```sql
-- Primary training sample dataset
CREATE TABLE MauHoc (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    TextGoc     TEXT,       -- Raw string: "[1b] 3x∅20, L= 11700"
    ChiSoSo     INTEGER,    -- 0-based token index (0, 1, 2...)
    GiaTri      INTEGER,    -- Numerical token value: 1, 3, 20, 11700
    Feature     TEXT,       -- Contextual NLP feature vector flags
    Nhan        TEXT,       -- Target semantic label
    Nguon       TEXT,       -- "seed" (45,006 rows) or "user" (14 rows)
    ThoiGian    TEXT,       -- Timestamp
    Hash        TEXT        -- MD5 hash for deduplication
);

-- User active correction audit log for reinforcement learning
CREATE TABLE LichSuSua (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    TextGoc     TEXT,       -- Raw annotation string
    Truoc       TEXT,       -- Prediction before correction
    Sau         TEXT,       -- Corrected canonical string
    ThoiGian    TEXT
);

-- Model training lineage and checkpoint registry
CREATE TABLE CauHinhModel (
    Ten         TEXT,       -- "SteelToken"
    DuongDan    TEXT,       -- Path to zipped model
    SoMau       INTEGER,    -- Sample count (e.g. 45048)
    ThoiGian    TEXT
);
```

### 3.3 Semantic Label Distribution
Analysis of the 45,020 samples in `mauhoc.db`:
- **`KC1`** (9,810 samples): Leg / hook length A of bent bar (mm).
- **`L`** (9,325 samples): Overall total bar length (mm).
- **`SL`** (9,243 samples): Bar quantity multiplier (e.g. `3` in `3∅20`).
- **`BO`** (6,195 samples): Extraneous number to ignore (drawing sheet number, revision tag).
- **`SH`** (4,339 samples): Rebar designation mark (Bar Mark, e.g. `1`, `2a`, `3`).
- **`KC2`** (3,970 samples): Leg / hook length B of bent bar.
- **`KC3`** (1,268 samples): Leg / hook length C of bent bar.
- **`DK`** (530 samples): Rebar diameter in millimeters ($\phi 10, \phi 12, \dots, \phi 32$).
- **`KC4`** (340 samples): Leg / hook length D of bent bar.

### 3.4 Contextual Feature Engineering Pipeline
Each numerical token within a rebar string is mapped to a space-delimited feature string:

| Feature Flag | Detection Logic | Semantic Meaning |
|---|---|---|
| `PREV_lbr` | Preceding character is `[` | Number immediately follows left square bracket (indicates `SH`). |
| `PREV_rbr` | Preceding character is `]` | Number immediately follows right square bracket. |
| `PREV_d` | Preceding character is `∅`, `d`, or `Ф` | Number follows diameter symbol (strong indicator for `DK`). |
| `PREV_x` | Preceding character is `x` or `*` | Number follows multiplication symbol (indicator for quantity `SL` or diameter). |
| `NEXT_x` | Subsequent character is `x` or `*` | Number precedes multiplication symbol (strong indicator for quantity `SL`). |
| `NEXT_b` | Subsequent character is letter `a`, `b`, `c` | Sub-mark suffix (indicates `SH` sub-designation). |
| `INRING_1` | Character index lies inside `[...]` | Token is within bounding brackets (almost certainly `SH`). |
| `AFTERL_1` | Token follows literal `L=` | Number appears after length indicator (strong indicator for `L`). |
| `DKCHUAN_1`| Token value $\in \{6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32\}$ | Value matches standard Vietnamese rebar diameter series. |
| `NDIGIT_n` | String length of token equals $n$ | Token length ($n=1$ for mark/count, $n=2$ for diameter, $n=4, 5$ for length). |
| `BEFORED_1`| Token precedes diameter symbol `∅` | Number is placed before diameter (strong indicator for `SL`). |
| `INSEQ_1`  | Token belongs to an arithmetic progression | Number is part of a consecutive bar mark series. |

### 3.5 ML.NET Prediction & Canonical Formatting
1. **Preprocessing (`ChuanHoa`)**: Converts variations of diameter glyphs ($\phi$, $\Phi$, $\varnothing$, `d`, `D`, `%%c`) into the canonical glyph `∅`.
2. **Tokenizer**: Splits string into alternating textual separators and numerical tokens, preserving positional offsets.
3. **Model Evaluation**: Featurizes each numerical token and executes `PredictionEngine<SteelTokenData, SteelTokenPrediction>` trained via `MulticlassClassification.Trainers.SdcaMaximumEntropy`.
4. **Canonical Assembly**: Assembles classified tokens into the standardized rebar notation:
   `SL={SL};DK={DK};KC1={KC1};KC2={KC2};KC3={KC3};KC4={KC4};SH={SH};L={L}`
5. **Human-in-the-Loop Feedback**: Corrections made by users in AutoCAD update the SQLite table `LichSuSua` and feed incremental retraining loops.
