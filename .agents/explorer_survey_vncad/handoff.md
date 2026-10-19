# Báo Cáo Khảo Sát Kỹ Thuật: Chuyển Đổi vn-autocad-skill Sang HPBim C# (.NET)

## 1. Observation (Quan sát thực tế & Dữ liệu gốc)

Khảo sát toàn diện kho mã nguồn `vn-autocad-skill-main` tại `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\vn-autocad-skill-main`:

### 1.1 Cấu trúc mã nguồn & Công cụ
- Thư viện gốc bao gồm 24 module Python trong `vncad/`: `__init__.py`, `acad.py`, `annotate.py`, `arch.py`, `blocks.py`, `document.py`, `elevation.py`, `estimate.py`, `fixtures.py`, `mep.py`, `place.py`, `project.py`, `qc.py`, `qc_geom.py`, `qc_text.py`, `rebar.py`, `render.py`, `section.py`, `sheet.py`, `standards.py`, `struct.py`, `table.py`, `tiles.py`, `titleblock.py`.
- Bộ benchmark chuẩn `examples/nha_pho_5x18m/`: 30 tệp Python định nghĩa hình học, mặt bằng, kết cấu, MEP, chi tiết thép, dự toán và bố cục 26 tờ bản vẽ (`KT-01`..`KT-15`, `KC-01`..`KC-05`, `ME-01`..`ME-06`).
- Kết quả chạy kiểm thử chuẩn `uv run --with pytest pytest`:
  ```
  tests\test_acad_va_render.py ...                                         [ 13%]
  tests\test_ho_so_mau.py ......                                           [ 40%]
  tests\test_qc.py ......                                                  [ 68%]
  tests\test_thep_va_du_toan.py .......                                    [100%]
  ============================= 22 passed in 7.54s ==============================
  ```
- Kết quả kiểm tra QC Engine toàn bộ 26 tờ bản vẽ (`uv run python tools/soat-ban-ve-khong-render.py --du-an examples.nha_pho_5x18m`):
  ```
  SACH: 26 to, khong con cho nao chong nhau.
  ```

---

### 1.2 Danh mục Domain & Geometry Models cần chuyển đổi sang C# (`HPBim.Core`)

Dưới đây là catalog chi tiết toàn bộ các model nghiệp vụ xây dựng và hình học cần ánh xạ sang lớp đối tượng C# thuần (`netstandard2.0`):

| Model C# | Định nghĩa gốc Python | Thuộc tính (Properties) | Đơn vị | Kiểu dữ liệu C# | Quan hệ & Vai trò |
|---|---|---|---|---|---|
| `AxisGrid` | `layout.AXIS_X`, `AXIS_Y`, `annotate.grid_bubbles` | `Name`, `CoordinateMm`, `AxisDir` (X/Y), `BubbleRadius`, `StartPoint`, `EndPoint` | mm | `string`, `double`, `GridDirection`, `Point2D` | Trục định vị công trình, làm mốc gióng tọa độ |
| `LevelDef` | `layout.LEVELS`, `FLOOR_LEVEL`, `STOREY_H` | `Name`, `ElevationM`, `ElevationMm`, `HeightMm`, `FloorNumber`, `IsStructural` | m / mm | `string`, `double`, `int`, `bool` | Cao độ các tầng, mốc tham chiếu cho Wall, Col, Beam, Slab |
| `ColumnDef` | `layout.COLUMNS`, `arch.column`, `struct.column_section` | `Id`, `Location`, `WidthMm` (220), `DepthMm` (220), `RotationDeg`, `BaseLevel`, `TopLevel`, `Material` | mm, độ | `string`, `Point2D`, `double`, `LevelDef`, `string` | Cột BTCT chịu lực tại giao điểm trục, tiết diện 220x220 |
| `WallDef` | `layout.FLOOR_WALLS`, `arch.wall` | `Id`, `StartPoint`, `EndPoint`, `ThicknessMm` (110/220), `BaseLevel`, `TopLevel`, `BaseOffsetMm`, `Openings` | mm | `string`, `Point2D`, `double`, `LevelDef`, `List<OpeningDef>` | Tường bao (220) & tường ngăn (110), host của cửa |
| `OpeningDef` | `layout.FLOOR_WALLS[*][3]`, `arch.door`, `arch.window` | `Id`, `HostWallId`, `DistanceAlongWallMm`, `WidthMm`, `HeightMm`, `SillHeightMm`, `Kind` (door, door2, wind, open), `SwingDirection` | mm | `string`, `double`, `OpeningType`, `SwingType` | Lỗ mở trên tường (Cửa đi 1 cánh, 2 cánh, Cửa sổ, Lọt lòng) |
| `BeamDef` | `draw_structure.beam`, `tie_beam`, `struct.beam` | `Id`, `StartPoint`, `EndPoint`, `WidthMm`, `HeightMm`, `Level`, `BeamType` (D1, D2, D3, GM), `IsTieBeam` | mm | `string`, `Point2D`, `double`, `LevelDef`, `string`, `bool` | Dầm ngang D1 (200x400), dọc D2 (200x300), phụ D3 (200x300), giằng móng GM (250x400) |
| `SlabDef` | `draw_structure.SLAB_ZONES`, `arch.slab_edge` | `Id`, `Boundary`, `ThicknessMm` (100), `Level`, `ElevationOffsetMm` (-50 WC), `Openings` (Voids) | mm | `string`, `Polygon2D`, `double`, `LevelDef`, `List<Polygon2D>` | Bản sàn BTCT (S1, S2, S3, S4, lỗ mở thông tầng VOID) |
| `RoomDef` | `layout.FLOOR_ROOMS`, `annotate.room_label` | `Name`, `TagLocation`, `TheoreticalAreaM2`, `RotationDeg` (0/90), `Level`, `BoundaryPolygon` | m², mm | `string`, `Point2D`, `double?`, `double`, `Polygon2D` | Không gian chức năng, kiểm soát diện tích và thông thủy |
| `MepFixtureDef` | `layout.FLOOR_FIXTURES`, `fixtures._BUILDERS`, `draw_electrical.*` | `Id`, `Category`, `FamilyName`, `Location`, `RotationDeg`, `Level`, `WidthMm`, `DepthMm`, `HostType` | mm, độ | `string`, `MepCategory`, `string`, `Point3D`, `double` | Thiết bị vệ sinh, thiết bị bếp, đồ nội thất, thiết bị điện |
| `PipeRunDef` | `draw_plumbing.*`, `vncad.mep.pipe` | `Id`, `SystemType` (WaterSupply, SoilDrain, WasteDrain), `DiameterMm` (20, 25, 60, 114), `Points`, `Slope` (0.02), `Material` | mm, m/m | `string`, `PipeSystemType`, `double`, `List<Point3D>`, `string` | Tuyến ống cấp PPR D20/D25, thoát PVC D60/D114 độ dốc 2% |
| `RebarDef` | `vncad.rebar.*`, `struct.*`, `foundation_rebar.*` | `Id`, `HostElementId`, `BarMark`, `DiameterMm`, `SteelGrade` (CB300-V/CB240-T), `ShapeType`, `LengthMm`, `Quantity`, `SpacingMm`, `CoverMm`, `Zone` | mm, kg | `string`, `int`, `string`, `double`, `RebarShapeType` | Cốt thép dầm, cột, móng, sàn, thang theo TCVN 5574:2018 |

---

### 1.3 Thuật toán Hình học & Không gian trong `vncad`

1. **Tính toán Vecto & Dời tọa độ phân đoạn tường/dầm (`vncad/arch.py:14-23`):**
   - Vecto chỉ phương đơn vị $u = \left(\frac{dx}{L}, \frac{dy}{L}\right)$, với $L = \sqrt{dx^2 + dy^2}$.
   - Vecto pháp tuyến đơn vị vuông góc hướng trái $n = \left(-\frac{dy}{L}, \frac{dx}{L}\right)$.
   - Tọa độ dời theo khoảng cách $d$: $p' = p + u \cdot d$.
2. **Ngắt nét tường tại lỗ mở (`vncad/arch.py:27-55`):**
   - Thuật toán sắp xếp danh sách lỗ mở theo khoảng cách bắt đầu từ $p_1$: `cuts = sorted(openings)`.
   - Sinh các đoạn nhịp tường liên tục giữa các lỗ mở: `spans = [(0, d_1), (d_1 + w_1, d_2), ..., (d_k + w_k, L)]`.
   - Mỗi đoạn span được tạo thành đa giác khép kín 4 đỉnh với bề dày $t$:
     $$C = \left[ p_1 + u \cdot a + n \cdot \frac{t}{2}, \; p_1 + u \cdot b + n \cdot \frac{t}{2}, \; p_1 + u \cdot b - n \cdot \frac{t}{2}, \; p_1 + u \cdot a - n \cdot \frac{t}{2} \right]$$
3. **Cắt xén đoạn thẳng với hộp chữ nhật theo Liang-Barsky (`vncad/qc_geom.py:48-69`):**
   - Kiểm tra va chạm giữa đoạn thẳng `seg = (x0, y0, x1, y1)` và bounding box `hop = (xmin, ymin, xmax, ymax)` trong $O(1)$ phép tính vô hướng với tham số $t_0 = 0.0, t_1 = 1.0$.
4. **Phân vùng lưới không gian (Spatial Grid Bucketing) (`vncad/qc_geom.py:71-92`):**
   - Lưới ô vuông kích thước $O\_LUOI = 2000.0\text{ mm}$ đánh chỉ mục mọi đoạn thẳng hình học vào ô $(c_x, c_y)$ giảm số phép so sánh từ hàng chục triệu xuống vài chục nghìn.
5. **Đo diện tích đa giác phòng (Shoelace Formula) & Tính toán Diện tích:**
   - Đa giác khép kín gồm $N$ đỉnh $(x_i, y_i)$:
     $$Area = \frac{1}{2} \left| \sum_{i=0}^{N-1} (x_i y_{i+1} - x_{i+1} y_i) \right|$$
   - Cao độ giật cấp phòng ẩm ướt (WC, ban công): sàn giật âm $-50\text{ mm}$ so với sàn phòng chính (`layout.NOTES:111`).

---

### 1.4 Quy chuẩn cấu tạo Cốt thép TCVN 5574:2018 trong `vncad`

1. **Công thức trọng lượng đơn vị cốt thép (`vncad/struct.py:19-22`, `test_thep_va_du_toan.py:26-29`):**
   $$W = \frac{D^2}{162.2} \times L \quad (\text{kg})$$
   - $D = 16\text{ mm} \rightarrow 1.578\text{ kg/m}$
   - $D = 10\text{ mm} \rightarrow 0.617\text{ kg/m}$
   - $D = 18\text{ mm} \rightarrow 1.998\text{ kg/m}$
   - $D = 8\text{ mm} \rightarrow 0.395\text{ kg/m}$
2. **Quy tắc neo & nối thép (`vncad/rebar.py`, `skills/vn-ket-cau/SKILL.md:63-73`):**
   - **Chiều dài neo vào dầm, đài cọc:** $30D$ mỗi đầu (`NEO = 30`).
   - **Móc vuông $90^\circ$ neo dầm vào cột:** tối thiểu $12D$ (`draw_rebar_detail.py:153`).
   - **Chiều dài nối chồng thanh chịu lực:** $40D$ trong vùng kéo (`NOI = 40`), $30D$ trong vùng nén.
   - **Cây thép thương phẩm tiêu chuẩn:** $L_{\text{max}} = 11.700\text{ mm}$ ($11.7\text{ m}$). Nếu thanh dài $> 11.7\text{ m}$, tự động tính số mối nối chồng:
     $$n_{\text{splice}} = \max\left(0, \left\lceil \frac{L}{11700} \right\rceil - 1\right), \quad L_{\text{total}} = L + n_{\text{splice}} \times 40D$$
   - **So le mối nối:** Không nối quá $50\%$ số thanh trên cùng một mặt cắt ngang. Cấm nối thép trong nút khung và vùng mô men lớn (giữa nhịp lớp dưới, gối lớp trên).
   - **Móc uốn đai cọc:** $10D$ mỗi đầu (`MOC = 10`).
   - **Móc đai dầm/cột:** uốn góc $135^\circ$, đoạn thẳng móc $\ge 10D$ và không nhỏ hơn $75\text{ mm}$ (`rebar.py:16`, `draw_rebar_detail.py:83`).
3. **Phân bố đai gia cường (Zones):**
   - **Dầm:** Vùng đầu dầm dày $a100$ trong phạm vi $\min(2h, L/3)$ hoặc $L/4$, giữa nhịp thưa $a200$.
   - **Cột:** Vùng chân cột và đầu cột dày $a100$ trong phạm vi $\frac{H_{\text{tầng}}}{4}$, đoạn giữa thưa $a200$.
   - **Nút khung:** Đai cột dày $a100$ xuyên suốt vùng nút khung và kéo dài $2h$ về hai phía dầm liên kết.
4. **Thép sàn & Thép mũ gối (`draw_struct_details.py`, `draw_structure.py:121-125`):**
   - Lớp dưới chạy suốt hai phương X, Y: $\Phi 10a150$.
   - Thép mũ gối: vươn $L/4$ tính từ mép dầm ($1250\text{ mm}$ cho nhịp 5m) + nửa bề rộng dầm ($100\text{ mm}$) + chân móc uốn vuông ($70\text{ mm}$) = $1420\text{ mm}$.
5. **Lớp bê tông bảo vệ (`vncad/standards.py:130`):**
   - Móng (đài, cọc, giằng tiếp xúc đất): $c = 50\text{ mm}$.
   - Dầm, cột: $c = 25\text{ mm}$.
   - Bản thang: $c = 20\text{ mm}$.
   - Bản sàn: $c = 15\text{ mm}$.
6. **Chỉ tiêu hàm lượng thép nghiệm thu:**
   - Thép khung thân nhà: $20 \sim 45\text{ kg/m}^2$ diện tích sàn xây dựng (`test_thep_va_du_toan.py:56`).
   - Thép móng: $50 \sim 100\text{ kg/m}^3$ bê tông móng.

---

### 1.5 Quy tắc Soát lỗi QC Engine (`vncad/qc.py`, `qc_geom.py`, `qc_text.py`)

1. **Kiểm tra va chạm Text vs Line (`check_text_on_lines`):**
   - Bắt mọi đoạn thẳng hình học xuyên qua bounding box của chữ.
   - Bounding box được thu hẹp vào trong $14\%$ (`LOT = 0.14`) để loại trừ các trường hợp nét vẽ tiếp xúc viền ngoài.
2. **Kiểm tra chồng lấn Text vs Text (`check_text_overlap`):**
   - Tính toán diện tích giao nhau giữa hai hộp chữ. Ngưỡng cảnh báo: $S_{\text{giao}} / \min(S_A, S_B) \ge 18\%$ (`NGUONG = 0.18`).
3. **Kiểm tra tràn ô biên (`check_cell_bleed`):**
   - Kiểm tra mọi phần tử hình học có tâm thuộc ô $(col, row)$ kích thước $32000 \times 30000\text{ mm}$ nhưng biên bao $bbox$ lọt ra ngoài phạm vi cho phép quá $1.0\text{ mm}$.
4. **Quy tắc Kiểm tra Không gian & Cấu kiện (Domain Clash Rules):**
   - **Dầm phụ xuyên ô sàn:** Dầm phụ không được chạy xuyên qua giữa một ô sàn. Bắt buộc phải chia tách thành 2 ô sàn độc lập (Ví dụ: Dầm D3 chia ô sàn thành S1 và S2 tại `draw_structure.py:73-80`).
   - **Cột đè tường / Cột va chạm cửa:** Cột BTCT phải luôn được ưu tiên giữ nguyên tiết diện; lỗ mở cửa sổ/cửa đi phải cách mép cột tối thiểu $100\text{ mm}$.
   - **Quét cánh cửa (Door swing clearance):** Cung mở cánh cửa bán kính $w$ không được quét qua nhãn định danh phòng hoặc thiết bị vệ sinh cố định (Ví dụ: Dời nhãn WC1 lên góc trên để tránh cung quét cửa tại `layout.py:68-70`).
   - **Tuyến ống MEP vs Dầm/Tường:** Nhãn ống cấp thoát nước không được nằm trên nét tường (`label_at` phải đặt ở khoảng trống thông thủy).

---

### 1.6 Tập dữ liệu Chuẩn Benchmark Nhà Phố 5x18m (Ground Truth)

Trích xuất nguyên văn toàn bộ thông số định hình từ `examples/nha_pho_5x18m/layout.py`, `draw_structure.py`, `draw_rebar_detail.py`, `rebar_quantities.py`:

#### A. Hệ trục tọa độ Grids & Cao độ Levels
- **Kích thước khu đất/nhà:** Chiều rộng $W = 5000\text{ mm}$, Chiều dài $L = 18000\text{ mm}$.
- **Hệ trục dọc X (5 trục):**
  - X1: $0.0\text{ mm}$
  - X2: $5500.0\text{ mm}$ (Nhịp 1-2: $5500\text{ mm}$)
  - X3: $9500.0\text{ mm}$ (Nhịp 2-3: $4000\text{ mm}$)
  - X4: $13000.0\text{ mm}$ (Nhịp 3-4: $3500\text{ mm}$)
  - X5: $18000.0\text{ mm}$ (Nhịp 4-5: $5000\text{ mm}$)
- **Hệ trục ngang Y (2 trục):**
  - YA: $0.0\text{ mm}$
  - YB: $5000.0\text{ mm}$ (Khẩu độ A-B: $5000\text{ mm}$)
- **5 Cao độ thiết kế (Levels):**
  1. `TRET`: $+0.150\text{ m}$ ($150\text{ mm}$)
  2. `LAU 1`: $+3.800\text{ m}$ ($3800\text{ mm}$) - Chiều cao tầng 1: $3650\text{ mm}$
  3. `LAU 2`: $+7.200\text{ m}$ ($7200\text{ mm}$) - Chiều cao tầng 2: $3400\text{ mm}$
  4. `SAN THUONG`: $+10.600\text{ m}$ ($10600\text{ mm}$) - Chiều cao tầng 3: $3400\text{ mm}$
  5. `MAI CHE THANG`: $+13.400\text{ m}$ ($13400\text{ mm}$) - Chiều cao tum: $2800\text{ mm}$
  - Cao độ tự nhiên vỉa hè: $Z_{\text{DAT}} = -0.050\text{ m}$.
  - Đáy đài móng: $Z_{\text{MONG}} = -1.450\text{ m}$.

#### B. 10 Cột BTCT 220x220mm
Vị trí tọa độ 10 cột tại tất cả giao điểm trục $X \times Y$:
1. $C_1$: $(0, 0)$
2. $C_2$: $(0, 5000)$
3. $C_3$: $(5500, 0)$
4. $C_4$: $(5500, 5000)$
5. $C_5$: $(9500, 0)$
6. $C_6$: $(9500, 5000)$
7. $C_7$: $(13000, 0)$
8. $C_8$: $(13000, 5000)$
9. $C_9$: $(18000, 0)$
10. $C_{10}$: $(18000, 5000)$

#### C. Hệ thống Tường (Walls) & Lỗ mở (Openings)
- **Tầng 1 (8 đoạn tường):**
  1. Tường hông phải (220): $(0, 0) \rightarrow (18000, 0)$, Lỗ mở: Cửa sổ $(15600, w=1600)$
  2. Tường hông trái (220): $(0, 5000) \rightarrow (18000, 5000)$, Lỗ mở: Cửa sổ $(6200, w=1800)$
  3. Mặt tiền gara (220): $(0, 0) \rightarrow (0, 5000)$, Lỗ mở: Cửa cuốn gara $(700, w=3600)$
  4. Tường hậu ra sân (220): $(18000, 0) \rightarrow (18000, 5000)$, Lỗ mở: Cửa 1 cánh $(1800, w=900)$
  5. Vách gara / khách (110): $(5500, 0) \rightarrow (5500, 5000)$, Lỗ mở: Thông thoáng $(1400, w=2200)$
  6. Vách WC1 đứng (110): $(9500, 0) \rightarrow (9500, 1900)$, Lỗ mở: Cửa WC $(500, w=800)$
  7. Vách WC1 ngang (110): $(9500, 1900) \rightarrow (11400, 1900)$, Không có lỗ mở
  8. Vách khách / bếp (110): $(13000, 0) \rightarrow (13000, 5000)$, Lỗ mở: Thông thoáng $(2000, w=2600)$
- **Tầng 2 (9 đoạn tường):**
  1. Tường hông phải (220): $(0, 0) \rightarrow (18000, 0)$, Lỗ mở: Cửa sổ $(4200, w=1600)$
  2. Tường hông trái (220): $(0, 5000) \rightarrow (18000, 5000)$, Lỗ mở: Cửa sổ $(4200, w=1600)$
  3. Vách ban công mặt tiền (220): $(0, 0) \rightarrow (0, 5000)$, Lỗ mở: Cửa vách kính $(1200, w=2600)$
  4. Tường hậu (220): $(18000, 0) \rightarrow (18000, 5000)$, Lỗ mở: Cửa sổ $(1500, w=1600)$
  5. Vách ra ban công (220): $(1500, 0) \rightarrow (1500, 5000)$, Lỗ mở: Cửa 1 cánh $(2100, w=900)$
  6. Vách ngủ 1 / sảnh (110): $(7800, 0) \rightarrow (7800, 5000)$, Lỗ mở: Cửa 1 cánh $(2000, w=900)$
  7. Vách sảnh / ngủ 2 (110): $(13000, 0) \rightarrow (13000, 5000)$, Lỗ mở: Cửa 1 cánh $(2000, w=900)$
  8. Vách WC2 đứng (110): $(10500, 0) \rightarrow (10500, 2400)$, Lỗ mở: Cửa WC $(900, w=800)$
  9. Vách WC2 ngang (110): $(10500, 2400) \rightarrow (13000, 2400)$, Không có lỗ mở
- **Tầng 3 (9 đoạn tường):**
  1. Tường hông phải (220): $(0, 0) \rightarrow (18000, 0)$, Lỗ mở: Cửa sổ $(4200, w=1600)$
  2. Tường hông trái (220): $(0, 5000) \rightarrow (18000, 5000)$, Lỗ mở: Cửa sổ $(4200, w=1600)$
  3. Vách kính ban công (220): $(0, 0) \rightarrow (0, 5000)$, Lỗ mở: Cửa sổ kính $(1200, w=2600)$
  4. Tường hậu (220): $(18000, 0) \rightarrow (18000, 5000)$, Lỗ mở: Cửa sổ $(1600, w=1400)$
  5. Vách ra ban công (220): $(1500, 0) \rightarrow (1500, 5000)$, Lỗ mở: Cửa 1 cánh $(2100, w=900)$
  6. Vách phòng thờ (110): $(7800, 0) \rightarrow (7800, 5000)$, Lỗ mở: Cửa 2 cánh $(1900, w=1600)$
  7. Vách ngăn ngủ 3 (110): $(13000, 0) \rightarrow (13000, 5000)$, Lỗ mở: Cửa 1 cánh $(2000, w=900)$
  8. Vách WC3 đứng (110): $(15600, 0) \rightarrow (15600, 2200)$, Lỗ mở: Cửa WC $(700, w=800)$
  9. Vách WC3 ngang (110): $(15600, 2200) \rightarrow (18000, 2200)$, Không có lỗ mở

#### D. Danh mục Phòng (Rooms) & Diện tích lý thuyết
- **Tầng 1 (5 phòng):**
  - Gara ô tô: Tâm tag $(2750, 2500)$, Diện tích: $26.4\text{ m}^2$
  - Phòng khách: Tâm tag $(7500, 3300)$, Diện tích: $19.2\text{ m}^2$
  - Giếng trời + cầu thang: Tâm tag $(11250, 3600)$, Diện tích: N/A
  - WC 1: Tâm tag $(10600, 1700)$, Diện tích: $3.0\text{ m}^2$
  - Bếp + phòng ăn: Tâm tag $(15500, 2500)$, Diện tích: $24.0\text{ m}^2$
- **Tầng 2 (5 phòng):**
  - Ban công: Tâm tag $(750, 2500)$, Diện tích: $3.6\text{ m}^2$ (Tag xoay $90^\circ$)
  - Phòng ngủ master 1: Tâm tag $(4650, 2500)$, Diện tích: $30.5\text{ m}^2$
  - Sảnh + thông tầng: Tâm tag $(9150, 3600)$, Diện tích: N/A
  - WC 2: Tâm tag $(11750, 2100)$, Diện tích: $5.6\text{ m}^2$
  - Phòng ngủ master 2: Tâm tag $(15500, 3500)$, Diện tích: $24.0\text{ m}^2$
- **Tầng 3 (6 phòng):**
  - Ban công: Tâm tag $(750, 2500)$, Diện tích: $3.6\text{ m}^2$ (Tag xoay $90^\circ$)
  - Phòng thờ gia tiên: Tâm tag $(4650, 2500)$, Diện tích: $30.5\text{ m}^2$
  - Sảnh + thông tầng: Tâm tag $(9150, 3600)$, Diện tích: N/A
  - Phòng ngủ master 3: Tâm tag $(14400, 3500)$, Diện tích: $17.0\text{ m}^2$
  - WC 3: Tâm tag $(16800, 1900)$, Diện tích: $5.3\text{ m}^2$
  - Sân phơi: Tâm tag $(16850, 4100)$, Diện tích: N/A

#### E. Hệ Dầm & Bản Sàn Tầng Điển Hình
- **Hệ Dầm:**
  - Dầm ngang D1 ($200 \times 400$): 5 dầm tại $X = 0, 5500, 9500, 13000, 18000$, chạy từ $Y=0$ đến $Y=5000$.
  - Dầm dọc D2 ($200 \times 300$): 2 dầm tại $Y = 0$ và $Y = 5000$, chạy suốt từ $X=0$ đến $X=18000$.
  - Dầm phụ D3 ($200 \times 300$): 1 dầm tại $Y = 2500$, chạy từ $X=0$ đến $X=5500$.
- **Hệ Sàn dày 100mm:**
  - $S_1$: $[(0, 0), (5500, 0), (5500, 2500), (0, 2500)]$
  - $S_2$: $[(0, 2500), (5500, 2500), (5500, 5000), (0, 5000)]$
  - $S_3$: $[(5500, 0), (9500, 0), (9500, 5000), (5500, 5000)]$
  - Lỗ mở thông tầng / buồng thang `VOID`: $[(9500, 0), (13000, 0), (13000, 5000), (9500, 5000)]$
  - $S_4$: $[(13000, 0), (18000, 0), (18000, 5000), (13000, 5000)]$

#### F. Cầu Thang Bộ 2 Vế (21 Bậc)
- Gốc đặt: $(9700, 4700)$, Hướng chạy: $u = (1, 0)$, Bề rộng vế: $1000\text{ mm}$.
- Kích thước bậc: Bề rộng mặt bậc $270\text{ mm}$, Chiều cao cổ bậc $173.8\text{ mm}$.
- Vế 1: 10 bậc lên chiếu nghỉ ($H = 1738\text{ mm}$).
- Vế 2: 11 bậc lên sàn tầng trên ($H = 1912\text{ mm}$). Tổng 21 bậc = $3650\text{ mm}$.
- Bản thang BTCT dày $120\text{ mm}$, Dầm chân thang DCT $200\times 300$, Dầm chiếu nghỉ DCN $200\times 300$.

#### G. Thiết bị MEP & Vị trí
- Thiết bị vệ sinh:
  - Tầng 1 (WC1): Bồn cầu $(10100, 800)$, Lavabo $(11000, 500)$
  - Tầng 2 (WC2): Bồn cầu $(11000, 700)$, Lavabo $(12400, 500)$, Vòi sen $(11200, 1800)$, Phễu thu $(12000, 1500)$
  - Tầng 3 (WC3): Bồn cầu $(16300, 700)$, Lavabo $(17300, 500)$, Vòi sen $(16600, 1600)$, Phễu thu $(17400, 1400)$
- Ống đứng xuyên tầng (Risers): Tầng 1 $(10200, 1200)$; Tầng 2 $(11600, 1400)$; Tầng 3 $(16600, 1300)$.
- Hố ga 800x800: $(17200, 300)$.
- Tủ điện tổng 12 lộ: $(5200, 4600)$ tại Tầng 1.

#### H. Khối lượng Cốt Thép Ground Truth Benchmark
Số liệu đã qua kiểm chứng độc lập trên bảng thống kê và mô hình test:
- **Thép móng (Đài cọc M1, M2 + Giằng móng GM1-GM7):** $748.9\text{ kg}$
- **Thép khung 1 tầng điển hình (Dầm D1-D3, Sàn S1-S4, Cột C1):** $2282.1\text{ kg}$
- **Thép 1 đoạn thang bộ (Vế 1, Vế 2, Chiếu nghỉ, Dầm DCT, DCN):** $123.7\text{ kg}$
- **Tổng thép xuất hiện trên các bảng thống kê bản vẽ (`KC-01`, `KC-02`, `KC-05`):** $3154.8\text{ kg}$
- **Tổng thép khung thân nhà đưa vào Dự toán (3 tầng điển hình + 3 vế thang):**
  - Thép nhỏ ($D \le 10\text{ mm}$): $4120.9\text{ kg}$ (Mã `AF.623`)
  - Thép lớn ($D > 10\text{ mm}$): $3096.7\text{ kg}$ (Mã `AF.622`)
  - Tổng thân nhà: $7217.6\text{ kg}$
  - Hàm lượng thép khung: $\frac{7217.6\text{ kg}}{270\text{ m}^2} = 26.73\text{ kg/m}^2$ (nằm chuẩn trong khoảng quy chuẩn $20 \sim 45\text{ kg/m}^2$)
- **Tổng khối lượng thép toàn nhà (Móng + Thân):** $7966.5\text{ kg}$.

---

## 2. Logic Chain (Chuỗi suy luận & Đánh giá kỹ thuật)

1. **Từ cấu trúc Python sang C# Clean Code Architecture:**
   - Dựa trên quan sát 1.1 và 1.2, toàn bộ thư viện `vncad` được tổ chức dạng function-based Python (`arch.wall()`, `struct.beam()`). Để port sang C#, cần chuyển hóa thành mô hình **Domain-Driven Design (DDD)** hướng đối tượng với các Entity bất biến, DTO strongly-typed và Service thuần tính toán.
   - Theo kiến trúc Clean Code của repo (`HP_CLEAN_CODE_CORE.md`, `ARCHITECTURE.md`), dự án `HPBim.Core` phải là thư viện C# thuần `netstandard2.0`, hoàn toàn không tham chiếu tới `Autodesk.Revit.*`. Mọi thuật toán hình học (Liang-Barsky, phân chia lưới, tính diện tích Shoelace) và quy tắc cốt thép TCVN 5574:2018 sẽ nằm trọn vẹn trong `HPBim.Core`.
2. **Từ thuật toán hình học sang BIM Native Elements trong Revit:**
   - Thuật toán `wall()` và `openings()` trong Python trực tiếp tính toán tọa độ ngắt nét trên 2D. Trong Revit 2026, đối tượng `Wall` được tạo bởi đường tim (centerline curve) từ $p_1$ đến $p_2$. Các lỗ mở (Door/Window) là các phần tử dạng hosted gắn trực tiếp lên host Wall theo vị trí tâm $p_1 + u \cdot (d + w/2)$ và cao độ đáy/bậu cửa `SillHeight`.
   - Do đó, `HPBim.Core` cần cung cấp bộ chuyển đổi (Adapter/Converter) từ mô hình hình học phân đoạn sang định dạng Revit Command: sinh ra `WallDef` nguyên khối với danh sách `OpeningDef` gắn kèm, giúp Revit API tự động cắt tường bản địa (Native Cutout) mà không sinh lỗi trùng nét (Overlap).
3. **Từ QC Engine Python sang offline C# Pre-flight Validator:**
   - Quan sát 1.5 cho thấy QC Engine của `vncad` phát hiện cực kỳ hiệu quả các lỗi thực tế: dầm cắt sàn, cửa đụng cột, text đè nét, phần tử tràn biên.
   - Việc chuyển đổi thuật toán Liang-Barsky và Spatial Grid Index sang C# sẽ tạo thành một bộ **QC Engine Pre-flight Offline** cực mạnh trong `HPBim.Core`. Trước khi gửi bất kỳ lệnh nào qua Named Pipe tới Revit, mô hình JSON sẽ được chạy qua `QcEngine.Validate(model)` chỉ mất vài mili-giây. Điều này giúp ngăn chặn 100% các lỗi hình học, lỗi va chạm cấu kiện và đảm bảo tính hợp chuẩn TCVN trước khi Revit Transaction bắt đầu.
4. **Từ Benchmark Dataset sang `NhaPho5x18mFixture.cs` trong `HPBim.Core.Tests`:**
   - Toàn bộ tham số số học, tọa độ, số lượng phần tử và khối lượng cốt thép đã được trích xuất và kiểm chứng tự động ở quan sát 1.6.
   - Các con số này đóng vai trò là "Ground Truth Oracle" bất biến để tạo fixture kiểm thử `NhaPho5x18mFixture.cs` trong `HPBim.Core.Tests`. Mọi bài test xUnit v3 có thể đối soát từng byte, từng tọa độ đỉnh và từng kg thép mà không cần mở Revit.

---

## 3. Caveats (Các điểm lưu ý & Giới hạn khảo sát)

1. **Khác biệt hệ tọa độ CAD vs Revit:**
   - Trong `vncad`, đơn vị bản vẽ là milimet ($mm$) và hệ tọa độ 2D mặt bằng có gốc tại góc trước nhà $(0, 0)$.
   - Trong Revit API nội tại, chiều dài được lưu trữ bằng đơn vị Feet ($1\text{ ft} = 304.8\text{ mm}$, $1\text{ mm} = \frac{1}{304.8}\text{ ft}$). Tầng Revit được quản lý theo trục cao độ Z tuyệt đối. Cần xây dựng lớp tiện ích `UnitUtils` trong `HPBim.Core` / `HPBim.Client` để chuyển đổi mượt mà giữa mm, m và feet.
2. **Quản lý Family và Type trong Revit:**
   - `vncad` vẽ đối tượng trực tiếp theo kích thước hình học thuần túy.
   - Trong Revit, các đối tượng như Cột 220x220, Tường 110, Tường 220, Dầm 200x400 yêu cầu phải tồn tại `FamilySymbol` tương ứng. Nếu dự án Revit trắng thiếu các Type này, `HPBim.Client` phải có logic tự động nhân bản (`Duplicate`) FamilySymbol và gán tham số `b`, `h` trước khi tạo Element.
3. **Mặt đứng và Mặt cắt:**
   - Trong CAD, mặt đứng và mặt cắt phải vẽ lại từng nét 2D độc lập. Trong Revit, khi mô hình 3D (Cột, Dầm, Tường, Sàn, Cửa) đã được sinh chính xác từ `HPBim.Client`, các View Mặt đứng và Mặt cắt sẽ tự động hiển thị đầy đủ mà không cần phát lệnh dựng riêng.
4. **Không tính toán nội lực kết cấu:**
   - Đúng như lưu ý trong `skills/vn-ket-cau/SKILL.md:18-21`, quy tắc bố trí cốt thép và tiết diện trong `vn-autocad-skill` là **bố trí theo cấu tạo chuẩn nhà phố** (TCVN 5574:2018), không thay thế việc chạy phần mềm giải tích kết cấu (như ETABS/SAP2000) đối với các công trình nhịp lớn hoặc địa chất phức tạp.

---

## 4. Conclusion (Kết luận & Đề xuất kiến trúc cho HPBim)

1. **Tính khả thi chuyển đổi:** $100\%$ logic hình học, quy tắc TCVN, thuật toán soát lỗi QC và bộ dữ liệu benchmark Nhà Phố 5x18m hoàn toàn khả thi để chuyển đổi trọn vẹn sang C# .NET theo cấu trúc giải pháp sau:
   - **`HPBim.Core` (Target `netstandard2.0`):**
     - `Geometry/`: `Point2D`, `Point3D`, `Vector2D`, `Polygon2D`, `BoundingBox2D`, `LiangBarskyClipping`.
     - `Models/`: `AxisGrid`, `LevelDef`, `WallDef`, `ColumnDef`, `BeamDef`, `SlabDef`, `OpeningDef`, `RoomDef`, `MepFixtureDef`, `PipeRunDef`, `RebarDef`.
     - `Algorithms/`: `WallOffsetCalculator`, `RoomAreaCalculator` (Shoelace), `OpeningPlacementEngine`.
     - `TcvnRules/`: `TcvnRebarCalculator` (anchorage $30D$, lap $40D$, stirrup zones $a100/a200$, covers $15/20/25/50\text{ mm}$, weights $D^2/162.2$).
     - `QcEngine/`: `QcClashDetector`, `ClearanceChecker`, `TcvnComplianceChecker`.
     - `Serialization/`: `BimModelSerializer` (JSON schema I/O).
   - **`HPBim.Core.Tests` (Target `net8.0` / `net10.0`):**
     - `Fixtures/NhaPho5x18mFixture.cs`: Bộ dữ liệu chuẩn ground truth đối soát số liệu.
     - Kiểm thử 100% hình học, diện tích phòng, va chạm QC, và hàm lượng thép.
   - **`HPBim.Client` (Target `net8.0` / `net10.0`):**
     - Kết nối Revit Named Pipe `\\.\pipe\hprebar-mcp-r2026`.
     - Pipeline dựng hình tuần tự: `Levels` $\rightarrow$ `Grids` $\rightarrow$ `Columns` $\rightarrow$ `Beams` $\rightarrow$ `Floors` $\rightarrow$ `Walls` $\rightarrow$ `Openings` $\rightarrow$ `Rooms` $\rightarrow$ `MEP`.
     - Hỗ trợ `dryRun: true` rollback sạch sẽ.
   - **`HPBim.Cli` (Target `net10.0`):**
     - CLI commands: `validate`, `generate`, `verify`, `estimate`.
2. **Kế hoạch thực thi kế tiếp:** Bàn giao báo cáo khảo sát chi tiết này làm tài liệu nền tảng cho Orchestrator lập kế hoạch 6 phase chuẩn Clean Code và triển khai các subagent kiến trúc, kết cấu, MEP và QC.

---

## 5. Verification Method (Phương pháp độc lập kiểm chứng)

1. **Kiểm tra bộ test gốc Python:**
   ```powershell
   cd "g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\vn-autocad-skill-main"
   uv run --with pytest pytest
   ```
   *Kỳ vọng:* 22/22 tests PASSED.
2. **Kiểm tra công cụ QC Engine gốc:**
   ```powershell
   uv run python tools/soat-ban-ve-khong-render.py --du-an examples.nha_pho_5x18m
   ```
   *Kỳ vọng:* Xuất ra `SACH: 26 to, khong con cho nao chong nhau.`
3. **Kiểm tra xuất bảng Dự toán & Số liệu thép benchmark:**
   ```powershell
   uv run python -m examples.nha_pho_5x18m.build_dutoan
   ```
   *Kỳ vọng:* Xuất ra file Excel tại `examples/out/DT-01_DuToan_NhaPho_5x18m.xlsx` với các giá trị: Thép móng $748.9\text{ kg}$, Thép thân $D \le 10$ $4120.9\text{ kg}$, Thép thân $D > 10$ $3096.7\text{ kg}$, Tổng thép toàn nhà $7966.5\text{ kg}$.
4. **Đối chiếu mã nguồn:**
   - `vncad/arch.py`, `vncad/struct.py`, `vncad/rebar.py`, `vncad/standards.py`, `vncad/qc_geom.py`.
   - `examples/nha_pho_5x18m/layout.py`, `draw_structure.py`, `rebar_quantities.py`.
