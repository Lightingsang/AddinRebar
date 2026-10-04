# Source evaluation — `04_quy_dinh_thep_dam.md` (Q:, 2290 dòng, chỉ đọc)

Ngày 2026-10-04. Đánh giá từng khẳng định của tài liệu nguồn so với bằng chứng của repo. Không sửa Q:.

## 0. Bằng chứng & ký hiệu

Thứ tự ưu tiên: bản vẽ Kata T2-DY7.dwg (đo, golden test) → template Kata.xlsm thật → code HPRebar (hiện thực luật đã đo) → TCVN 5574:2018 → chính tài liệu.

| Mã | Nguồn |
|---|---|
| DY7 | plans/261002-0930-kata-dy7-match/plan.md (luật 1–8, dòng 19–26) + reports/live-verify-dy7-dy14.md (dòng 8–17: C13/C14 2350/1850, E13/E14 4050–8700/4550–8200, D18/D17 850/1350, top main 30→16215 liền, đai dày 1400/1750/550) |
| DY14 | plans/261002-1420-kata-dy14-match/plan.md (K13–K16, dòng 19–22) |
| SEC | plans/261003-1900-kata-section-cad-drawing/reports/dwg-section-rules.md (mặt cắt: đai tim c=25, thanh tại c+(d+ds)/2, khe lớp 25, móc C Ø8a500) |
| TIE | plans/261002-1130-kata-layer-spacer-ties/plan.md (dòng 22: I8 bật/tắt radio đọc được; L1–L5 dòng 28–32) |
| P1 | plans/261001-0900-kata-beam-rules-phase1/plan.md — luật lấy TỪ CHÍNH tài liệu này (đai dày max(2h,0.25Ln), cắt bụng 0.15Ln, chân 15d, khe max(d,30)) rồi bị DY7 lật lại; dòng 16 "chuỗi DLL bị mã hoá"; dòng 23 bỏ chia 11.7 m (quyết định user) |
| RT | plans/260928-1259-kata-rebar-mvp/reports/rule-table.md (mapping ô; Kata nạp/lưu qua VBA `save_info`, VBA không chứa luật vẽ) |
| SET | HPRebar/HPRebar.Core/KataRebar/Models/KataSettings.cs (dòng 17–68, mặc định = Kata) |
| PAR | HPRebar/HPRebar.Core/KataRebar/Parsers/KataDamSheetParser.cs |
| XLSM | C:\kata_pro\Kata.xlsm (+ Kata ENG.xlsm, Kata-GCAD.xlsm) sheet `Dam`, đọc bản sao bằng openpyxl 2026-10-04: giá trị, comment, merge, data validation |
| T- | test HPRebar.Core.Tests/KataRebar/…: KataDy7DrawingTests (DY7T), KataDy14DrawingTests (DY14T), KataTopBarStaggerTests (STG), KataSpecRuleTests (SPEC), KataSideBarTests (SIDE) |

Verdict: ✅ đúng · ❌ sai · ⚠️ không kiểm chứng được (hoặc mâu thuẫn chưa giải) · ➖ ngoài phạm vi (tỷ lệ vẽ, lệnh CAD, UI, tác giả, add-in khác).

## 1. Khẳng định theo chương

### Ch. 1 — Tổng quan (L6–35)
| L | Khẳng định | V | Bằng chứng / giá trị đúng |
|---|---|---|---|
| 10–15 | Dầm chính/phụ, M âm gối, M dương nhịp, Q max gối | ✅ | cơ học chung |
| 32–35 | Lệnh `vd`/`vda`/`vdmc`/`vds` | ➖ | lệnh CAD |
| 35 | `vds` chia thanh 11 700 | ⚠️ | shop drawing, không đo; `vd` vẽ thanh liền (DY7: top main 30→16215) |

### Ch. 2 — Tiêu chuẩn (L39–70)
| L | Khẳng định | V | Bằng chứng / giá trị đúng |
|---|---|---|---|
| 42 | TCVN 5574:2018 Mục 8.1 uốn, 10.3 cấu tạo | ✅ | số mục hợp lý |
| 43 | TCVN 1651:2018, CB300-V/CB400-V/CB500-V, CB240-T | ✅ | (thực tế là 1651-1/-2:2018) |
| 47–49 | w ≥ max((Q−Qsw)/2qsw, 0.5h0, 20d, 500) "Mục 10.3.2" | ❌ | công thức trộn TCVN 356 cũ (20d, (Q−Qs)/2qsw); số hạng 500 mm không có trong TCVN; 10.3.2 không phải mục đoạn kéo dài |
| 57 | G1 = 500 là w tối thiểu TCVN | ❌ | G1 = bước lệch giữa các lớp gia cường trên (nhãn ENG E1 "Length of between additional bar on 2 layer", XLSM); DY7 luật 3; STG `Each_outer_row_reaches_500_further…` |
| 61–63 | Q ≤ φb1·Rb·b·h0, φb1 = 1 − 0.01Rb | ❌ | là TCVN 356:2005; 5574:2018 dùng Q ≤ 0.3·Rb·b·h0 |
| 64–67 | Qb = φb2Rbt b h0²/c ≥ φb3…; Qsw = qsw·c | ❌ | 2018: Qb = 1.5Rbt b h0²/c, 0.5…2.5 Rbt b h0; Qsw = 0.75 qsw c |
| 68 | h0 ≤ c ≤ 2h0 | ⚠️ | 2018: c ≤ 3h0 cho Qb, c ≤ 2h0 cho Qsw |
| 70 | Q tập trung trong 2h → 3 vùng đai | ⚠️ | 3 vùng đúng; độ dài vùng Kata = 0.25 L0 (xem Ch.6) |

### Ch. 3.1 — Lệnh (L76–87)
Toàn bộ ➖ (lệnh LISP/.NET, không kiểm). `damgiao` "đai treo 5f10a50" ⚠️.

### Ch. 3.2 — các luật nằm trong mô tả ô (chỉ phần có tác dụng tới thép)
| L | Khẳng định | V | Bằng chứng / giá trị đúng |
|---|---|---|---|
| 107, 912–913, 972, 115 | Thép gối lớp 1 cắt Ln/3 (+500) | ❌ | mọi hàng 13–16 cắt H5·L (0.25) của **nhịp bên đó**, gốc I5, làm tròn lên 50; DY7 luật 3; C14 = 450 + ceil50(0.25·5500) = 1850 |
| 110, 449–450, 488 | +G1 cộng vào thanh có cờ `keo` | ❌ | G1 không cộng cho thanh 1 lớp; chỉ hàng ngoài vươn ≥ G1 xa hơn hàng liền trong (C13 2350 = C14 + 500) |
| 111–118, 451–457, 927, 971, 1005–1008 | I1 tick → lớp 2 = lớp 1 − h; bỏ tick → cắt cùng chỗ | ❌ | DY7: bước lệch = 500 = G1 cả bên nhịp h 600 (E 8700/8200, G 11650/11150); I1 mẫu = False (XLSM) mà vẫn lệch. Ý nghĩa ô I1 ⚠️ |
| 118 | so le bậc thang "TCVN Mục 10.3.1.1" | ⚠️ | trích dẫn không khớp nội dung mục cover |
| 148 | ENG E2 = "Tension anchor length (d)" | ❌ | thực tế "Top bar length to support (d)" (Kata ENG.xlsm) |
| 151–155 | G2 = 40 → 40d, bảng 640…1280 | ✅ | SET/RT A1–A4: La trên = G2·d từ mép trong gối |
| 157–166 | L0,an = Rs·d/(4Rbond), Rbond = η1η2Rbt, η1 2.5, η2 1.0, Rs 350/435, Rbt B25 1.05, B30 1.15 | ✅ | TCVN 5574:2018 (công thức 10.1/10.2 hợp lý) |
| 169–170 | Lan = 1.2·L0,an cho thanh chịu kéo → 40d | ❌ | α = 1.0 cho neo thanh có gờ chịu kéo; 1.2 là hệ số **nối chồng**. 40d của Kata là quy ước, không suy ra từ đây |
| 177–180, 211–222 | Neo biên: đủ chỗ → thẳng; thiếu → bẻ 90°, chân = phần thiếu, tròn 25 | ✅ | SPEC `A_leg_bends_exactly_the_missing_length_by_default`, `A_leg_is_rounded_up_to_25…`; SET:59,65 (min leg 0, 25) |
| 183–186 | nối chồng kéo ≥ 40d; dầm phụ neo 40d vào dầm chính | ⚠️ | không đo; HPRebar không nối (thanh liền) |
| 243 | ENG E3 = "Bottom bar length to support (d)" | ✅ | XLSM ENG |
| 261–263 | α nén 0.7–0.8 → 26.6d ≤ 30d | ❌ | TCVN α = 0.75 nén; G3 trong Kata = neo **thép dưới** (nhãn ENG), không phải "vùng nén" |
| 268–272 | thép dưới vào cột biên G3·d, thiếu → bẻ lên | ✅ | RT A1–A4; DY7 chân dưới Ø18 cột 450 = 125 |
| 281 | I3 mặc định Kata.xlsm = "L từ mép cột" | ❌ | Kata.xlsm I3 = "L từ tâm cột"; ENG "L from edge of support"; GCAD "L từ tâm cột" |
| 280, 296, 439 | I3 nguồn N3:N6 (4 lựa chọn) | ❌ | DV I3 = N3:N5 (3); I5 = N3:N6 (4) |
| 288–291, 317–319 | gia cường bụng cắt H3·Ln = 0.2Ln, dài 0.6Ln | ❌ | hàng 17 (lớp trong) dừng min(H3·L theo I3 tròn lên 50, L/6 tròn **gần nhất** 50) từ mép gối; hàng 18 = hàng 17 − G1 (dài hơn). DY7 luật 5 + DY14 K14; D17 1350 = 450 + 900 |
| 292 | `keo` → thanh bụng vươn tới mép gối | ⚠️ | không quan sát |
| 346–348, 1857–1860 | h ≥ 700 bắt buộc cốt giá, s ≤ 400 | ✅ | TCVN (h > 700, ≤ 400, ≥ 0.1%A); SET:68 chỉ cảnh báo |
| 349 | ACI 9.7.2.3 min(d/6, 300) | ➖ | ngoài TCVN |
| 357–359 | neo cốt giá 10d–15d | ✅ (10d) | SET:33 = 10d; DY14T `Row_20_2f12_…anchored_10d` |
| 356 | cốt giá chạy từng khoang nhịp | ❌ | liên tục qua gối giữa cho các nhịp liền có cùng cốt giá (DY7 luật 8; SIDE `…run_on_through_E…`) |
| 361–362, 1183 | G5 = số lớp × 2 thanh; hàng 20 ghi đè; trống → G4/G5 | ✅ | PAR:93; DY14 K15 (hàng 20 nfd = n lớp); SIDE `Row_20_counts_layers_like_G5` |
| 401 | Δy cốt giá = (h − 2c − 2ds − (dt+db)/2)/(n/2+1) | ⚠️ | SEC: chia đều giữa −(c+ds+dTop) và −h+(c+ds+dBot) — gần nhưng không cùng công thức |
| 411–412 | h ≥ 700 tự bật cốt giá; < 700 tự ẩn | ❌ | Kata vẽ theo G5/hàng 20 bất kể h (DY7 h 500 có 2Ø12) |
| 425–429 | G5 dương → có móc C, âm → không, 0 → không cốt giá | ✅ | comment G5 (XLSM); PAR:93; SIDE `A_negative_G5…` |
| 431 | móc C Ø = G6 | ✅ | SEC (Ø8 = đai) |
| 447 | H5 0.25 / 0.33 / 0.2 | ✅ | XLSM H5 = 0.25 |
| 509–511 | khe ngang ≥ max(d,25) dưới, ≥ max(d,30) trên | ✅ | TCVN 5574:2018 (số mục 10.3.1.2 ⚠️) |
| 513–515 | b > 350 bắt buộc đai 4–6 nhánh "10.3.2.1" | ⚠️ | quy định cũ; 2018 không có ngưỡng 350 như vậy |
| 520–523 | ds ≥ 6; ≥ 0.25 dmax | ✅ | TCVN; "d>20 → ≥ 8" ⚠️ |
| 526 | đai ghép G6 + a → f10a150 | ✅ | DWG tag Ø8a100 |
| 529–530, 722–723 | b_đai = b − 2c + 2ds | ❌ | kích thước ngoài đai = b − 2c (c = lớp bảo vệ tới mặt ngoài đai); công thức +2ds sai |
| 531 | móc 135°, r ≥ 2.5ds, đuôi 7.5ds | ✅ (7.5ds) | SET:17–19 |
| 537–541 | khe đứng ≥ max(d,25); thép kê Ø25 dài b − 2c @2000 | ✅ / ⚠️ | SEC: khe lớp 25 (Ø18); kê không vẽ |
| 578, 584 | vùng đai dày L/4 (hoặc 2h) | ✅/❌ | đúng 0.25 L0; "2h" ❌ |
| 579–581 | s1 ≤ min(h/2,150) khi h > 450 | ❌ | luật cũ cho h ≤ 450; 2018: ≤ 0.5h0 và ≤ 300 |
| 638 | s2 ≤ 3h/4, ≤ 500 | ✅ | (0.75h0) |
| 640 | thực tế min(h/2, 250) | ⚠️ | thói quen, không TCVN |
| 589–590, 650–653, 670 | I8 = 1 "Bố trí đều với" J7; I8 = 2 "Giống đai ngoài" | ❌ | **ngược**: I8 = 1 "Giống đai ngoài", I8 = 2 "Bố trí đều với" J7 (TIE dòng 22, bật radio đọc I8); mặc định 2 → móc C a500 (SEC Ø8a500) |
| 693–699 | console rải đều G9 toàn chiều dài | ✅ | KataStirrupZoneLayout "A cantilever span gets one uniform zone" |
| 694 | console s ≤ min(h/2,150) TCVN | ⚠️ | không có trong 5574 |
| 713–717 | J9 "a" = mép → tâm thép chủ | ✅ | KataDetailingRuleBuilder (doc-comment); RT C1 |
| 717–720 | 50/25 hoàn hảo vì a = 25+10+12.5 | ⚠️ | trùng hợp số; code nâng a khi < b + ds + d/2 |
| 728 | J9 1 số: a_đai = a − d_đai/2 | ❌ | code: b = a − d/2 − ds (RT C2); comment Kata "30-cd/2" mơ hồ |
| 750 | Revit cover 25 mọi mặt | ➖ | add-in khác |

### Ch. 4 — Thép dọc (L1670–1732)
| L | Khẳng định | V | Bằng chứng / giá trị đúng |
|---|---|---|---|
| 1678–1679, 1713–1715 | Lớp 1 Ln/3 hoặc 0.25Ln; lớp 2 Ln/4 hoặc 0.2Ln | ❌ | mọi hàng H5·L, hàng ngoài +G1 (DY7 luật 3; STG `Every_row_reaches_H5_of_its_own_span…`) |
| 1681, 2016 | nối thép trên giữa nhịp L/3 | ➖ | không nối (thanh liền) |
| 1694–1702 | As cont ≥ 0.25 As gối / 0.5 As nhịp / 0.15% | ⚠️ | không TCVN 5574 (μmin 0.1%), không Kata |
| 1697 | Φ trên ≥ 16 | ⚠️ | thói quen |
| 1708 | lớp 2 cách lớp 1 ≥ max(d,30) | ⚠️ | Kata vẽ khe 25 (SEC); SET:62 mặc định 30 (lệch đã biết); mâu thuẫn L538/2249 (25) |
| 1716 | gối giữa nhịp lệch: lấy max(Ln1,Ln2) | ❌ | mỗi bên theo nhịp của bên đó (E13 4050 trái / 8700 phải, DY7) |
| 1720–1722 | lệch cắt giữa 2 lớp ≥ 500, tự kéo dài | ✅ | STG; chỉ đúng khi hiểu "ngoài ≥ trong + G1" |
| 1729–1732 | bụng cắt 0.15–0.20Ln, dài 0.6–0.7Ln | ❌ | min(H3·L, L/6) + hàng 18 −G1 |
| 1728 | gia cường bụng không vào cột | ✅ | thẳng, dừng trong nhịp |

### Ch. 5 — Neo (L1736–1779)
| L | Khẳng định | V | Bằng chứng / giá trị đúng |
|---|---|---|---|
| 1766–1767 | trên chạy tới mép xa − cover, bẻ 90° xuống | ✅ | KataAnchorage doc-comment; DY7 top legs 300 |
| 1768–1770 | tổng ≥ 40d, chân ≥ 15d–30d | ❌ | chân = đúng phần thiếu (min 0), tròn lên 25 khi vừa; SET:59 |
| 1772–1773 | dưới thẳng ≥ 20d; thiếu → chân ≥ 15d | ❌ | G3·d từ mép trong, chân = phần thiếu (DY7 dưới Ø18: 125 = 6.9d) |
| 1777–1778 | G2 = trên (và dưới khi kéo); G3 = nén qua gối giữa | ❌ | G2 = neo thép trên, G3 = neo thép dưới (nhãn ENG); qua bậc đáy cắt: nhịp nông thẳng G3·d |
| 1779 | gối tường ≥ 200/300 | ➖ | |

### Ch. 6 — Đai (L1783–1829)
| L | Khẳng định | V | Bằng chứng / giá trị đúng |
|---|---|---|---|
| 1792, 1803–1804, 2049, 2114, 2136, 1247 | vùng đai dày 2h (hoặc 0.25Ln) | ❌ | ceil50(0.25·L0) từ mép, hệ số h = 0, ≤ L/2 (SET:48–50; DY7 luật 6; DY7T `Dense_stirrup_zones_are_a_quarter_of_each_span`) |
| 1806 | s_dense ≤ min(h/4, 8d, 24ds, 100–150) | ⚠️ | ACI/EC8/9386, không 5574, không Kata |
| 1808 | đai đầu cách mép 50 | ✅ | RT S1–S3; KataStirrupZoneLayout |
| 1812 | s giữa ≤ min(h/2,250) | ⚠️ | thực tế; Kata: vùng giữa chia đều ≤ bước danh định |
| 1818 vs 2273 | móc 135° đuôi ≥ max(10d,75) / 7.5d | ⚠️ | tự mâu thuẫn; Kata 7.5ds (SET:19) |
| 1820–1823 | đai 2 U nối 30d (C24) | ⚠️ | comment C24 "Nhập 30 vẽ đai chống xoắn" — ý nghĩa 30 chưa đo |
| 1826–1829 | số nhánh theo b; s_leg ≤ 350 | ⚠️ | không TCVN 2018; Kata theo hàng 25–44 |

### Ch. 7 — Cốt giá (L1833–1871)
| L | Khẳng định | V | Bằng chứng / giá trị đúng |
|---|---|---|---|
| 1858 | bắt buộc h ≥ 700 "10.3.1.2" | ✅ (nội dung) / ⚠️ (số mục) | |
| 1860 | s ≤ min(h/2, 400) | ⚠️ | h/2 tự thêm |
| 1865–1867 | số lớp theo h | ⚠️ | Kata theo G5/hàng 20 |
| 1869–1870 | móc C mỗi cặp, d8a400 | ❌ | Ø = G6, bước J7 (a500) / I8 (SEC Ø8a500; SIDE `Each_layer_gets_C_ties_at_J7…`; TIE L4) |

### Ch. 8 — Dầm phụ / đai treo (L1875–1964)
| L | Khẳng định | V | Bằng chứng / giá trị đúng |
|---|---|---|---|
| 1903 | Asw,h = V(1 − hs/hprim)/Rsw | ⚠️ | gần TCVN (dùng h0 dầm chính) |
| 1937–1960 | 5f10a50 mỗi bên, vai bò 2f14 45°, bẻ ngang 150, không đai băng qua | ⚠️ | tab "Thép mặc định" Kata, chưa đo; K24 "*" xác nhận có "đai gia cường" tại nút |
| 1963–1964 | ≤ 200 từ mép cột → dồn đai một phía | ⚠️ | |
| — | gối là dầm (`bxh` hàng 11) = gối rộng b | thiếu | DY7 luật 2 (I 200) — tài liệu đặt `bxh` ở hàng 20 |

### Ch. 9 — Giật cấp (L1968–1997)
| L | Khẳng định | V | Bằng chứng / giá trị đúng |
|---|---|---|---|
| 1985–1989, 2061, 2120 | Δh ≤ 100 → uốn Z 1:6; > 100 → cắt | ❌ (đáy) / ⚠️ (đỉnh) | bậc đáy: uốn 1:6 khi Ø ≥ 16 và (bậc − Ø)/bề rộng gối ≤ 1/6, không thì cắt (DY14 K13: 82/350 cắt dù < 100; DY14T). Bậc đỉnh (hàng 19) chưa đo |
| 1989 | 2 đai kẹp ở điểm uốn | ⚠️ | không thấy trong DY7 |
| 1994–1996 | cắt: thanh cao bẻ xuống 40d, thanh thấp đâm 40d | ❌ (đáy) | đáy: nhịp sâu tới mặt xa − a bẻ lên G3·d, nhịp nông thẳng G3·d từ mặt gối (DY7 luật 1) |

### Ch. 10 — Đổi tiết diện & 11.7 m (L2001–2021)
| L | Khẳng định | V | Bằng chứng / giá trị đúng |
|---|---|---|---|
| 2005–2008 | đáy giật lên: sâu bẻ lên, nông neo 30d | ✅ (khi cắt) | DY7 luật 1; thiếu trường hợp uốn 1:6 |
| 2009–2011 | đổi bề rộng: gom 1:6 | ⚠️ | |
| 858, 2013–2021 | > 11.7 m tự nối so le, vùng nối, 50%, 1.3Llap | ❌ (vd/HPRebar) | Kata `vd` vẽ thanh liền 16.2 m (DY7); HPRebar bỏ chia 11.7 (P1 dòng 23). Shop drawing ⚠️ |

### Ch. 11 — Bảng tham số (L2027–2093)
| Hàng bảng | V | Ghi chú |
|---|---|---|
| 11.7 m / 8800 / 100d / nối / coupler Φ≥30 / ΔLap 40 / dung sai 50 | ❌/⚠️ | không áp dụng (thanh liền); giá trị tab Kata chưa đo |
| Lan,tens 40d "Mục 10.3.2", Llap,comp 30d "10.3.3" | ✅ giá trị / ⚠️ số mục | mâu thuẫn L157 (10.3.5.1) |
| Bẻ cổ chai Φ ≥ 16, 1:6 | ✅ | SET:30 + KataDetailingRules CrankSlope 6 (dùng cho bậc đáy, không cho nối) |
| w +500 "10.3.2" | ❌ | G1 = bước lệch lớp |
| Cắt gối L1 Ln/3, L2 Ln/4 | ❌ | H5·L + G1 |
| Cắt bụng 0.15–0.20 | ❌ | min(H3·L, L/6) |
| Làm tròn ke 25, cắt 50 | ✅ | SET:65, SET:27 (cắt trên: lên; trần L/6: gần nhất) |
| Đai dày 2h | ❌ | 0.25 L0 |
| S1 a100–150, S2 a200 | ✅ | mặc định G7/G8 |
| Móc 135°/7.5d, C 180°/7.5d | ✅ | SET:17–24 |
| Cốt giá h ≥ 700, neo 10d | ✅ | SET:68, SET:33 |
| Đai treo / vai bò / spec | ⚠️ | |
| Giật cấp Z ≤ 100 / tách 40d | ❌ | xem Ch.9 |
| 11.2 R02_BeamsRebar (L2063–2093) | ➖ | add-in khác — xem §3 |

### Ch. 12 — Mermaid (L2097–2171)
Lặp lại các lỗi Ch.4/6/9/10 (Ln/3, Ln/4+500, 2h, 0.15–0.2, Z ≤ 100, 11.7 m, d8a400). ❌ theo các dòng tương ứng.

### Ch. 13 — Lỗ xuyên dầm (L2175–2229)
Toàn bộ ⚠️/➖: không có trong TCVN 5574 dưới dạng này, Kata chưa đo, HPRebar không làm. Hàng 24 nhịp KHÔNG phải ô lỗ MEP (comment C24/K24).

### Ch. 14 — Khoảng hở (L2233–2257)
| L | Khẳng định | V | Ghi chú |
|---|---|---|---|
| 2239, 2243 | ngang ≥ max(d,25) dưới / max(d,30) trên | ✅ | TCVN |
| 2249 | đứng ≥ max(d,25) | ✅ | khớp SEC (25); SET:62 = 30 (cao hơn Kata) |
| 2251 | thép kê 25a2000 | ✅ (comment H6) | không vẽ |
| 2255–2257 | tab Thông số đặc thù 40/50/0 | ⚠️ | |

### Ch. 15 — Uốn (L2261–2286)
| L | Khẳng định | V | Ghi chú |
|---|---|---|---|
| 2265 | η1 thép trơn 1.5 | ✅ | TCVN |
| 2266–2268 | thép trơn chịu kéo móc 180°, ≥ 3d | ⚠️ | |
| 2271–2276 | đai 135°/7.5d, C 180°/7.5d | ✅ | SET |
| 2281 | R ≥ 2.5d (d<20), 3.5d (d≥20) | ⚠️ | TCVN: đường kính gối uốn thép gờ 5d / 8d → R 2.5d / 4d |
| 2286 | độ dãn uốn | ⚠️ | |
| 2289 | "kiểm chứng theo TCVN + Kata" | ❌ | không có bằng chứng kiểm chứng; nhiều số bị DY7 bác |

## 2. Ch. 3.2 — bảng ô sheet Dam (L89–1667)

Đối chiếu nội dung ô thật (XLSM) + parser (PAR). [P] = nghĩa trong tài liệu mâu thuẫn parser.

| Ô | Nghĩa theo tài liệu | V | Ghi chú |
|---|---|---|---|
| B1 / B2 (+M1:Q1, N2:Q2) | tỷ lệ dọc 1:50 / mặt cắt 1:25 (ENG 1:40) | ✅➖ | XLSM khớp |
| G1 | w kéo dài TCVN +500 | ❌ | bước lệch lớp gia cường trên; PAR:41 dương → dùng, khác → setting 500 + cảnh báo |
| I1 | checkbox "cách điểm cắt = h" | ⚠️ | ô có (False); nghĩa chưa đo; tác dụng mô tả bị DY7 bác |
| G2 / G3 | neo kéo 40d / nén 30d | ✅ giá trị, ❌ tên | = neo thép trên / dưới (ENG) |
| B3, B4 | tên dầm B01, số cấu kiện 1 | ✅ | PAR:29–31 |
| H3 + I3 | cắt bụng 0.2 + gốc đo; mặc định "mép cột" | ✅/❌ | H3 ✅ (dùng cho hàng 17, trần L/6); I3 Kata.xlsm = "tâm cột" ❌; DV N3:N5 |
| H5 + I5 | cắt gối 0.25 + "mép cột" | ✅ | dùng cho **mọi** hàng 13–16 (PAR:44, DY7 luật 3) |
| K2:K8, L9 | thông tin tác giả, URL | ➖ | L9 không có trong Kata.xlsm |
| G4 / G5 | Ø cốt giá 12 / số lớp 2, âm = không móc C | ✅ | PAR:89–102 |
| B5 / B6 | h 1100 / b 500 | ✅ | |
| G6 | Ø đai 10 | ✅ | |
| H6 | thép kê "25@2000" | ✅ (comment) | Kata.xlsm H6 trống, ENG = 25; L950 gọi nhầm "I6" |
| B7 | h sàn, cú pháp `*h1;down1/h2;down2` | ⚠️ [P] | cú pháp từ sách HDSD chưa kiểm; PAR:33 đọc B7 bằng `GetDouble` → chuỗi cú pháp mất (= 0) |
| G7 / G8 / G9 | a150 gối / a200 giữa / 150 console | ✅ | PAR:62–64 |
| H7, J7 | H7 = False, J7 = a500 bước đai gia cường | ✅ | |
| I8 | 1 = đều J7, 2 = giống đai ngoài | ❌ [P] | ngược (TIE dòng 22; PAR:65–68 đúng) |
| B8 / B9 / B10 | tên trục / lệch trục −100 / cao trình +3.300 | ✅ | |
| J9 | `a/b`: a mép→tâm thép chủ, b bảo vệ đai | ✅ | nhánh 1 số: công thức tài liệu ❌ (code b = a − d/2 − ds) |
| hàng 10 C… | "Cột"/"Nhịp" xen kẽ, cột 2k+1 / 2i+2 | ✅ | PAR:115–137; dừng ở ô hàng 11 trống đầu tiên (không phải "nhịp = 0") |
| 798–816 | danh sách nghĩa hàng theo gối/nhịp | ❌ [P] | sai ≥ 7 mục: gối h12 "cột trên" (thật: h19), gối h23 "dầm phụ" (thật: lệch trục), nhịp h21 "b×h" (thật: bậc đáy;thép), nhịp h23 "dầm phụ/cột cấy" (thật: bước đai gia cường), nhịp h24 "lỗ MEP" (thật: không; gối h24 = 30/\*), h25–27 "U/C" (thật: cặp loại/thanh 25–44), h28 "dầm phụ giằng" (thật: A28 = 1). Tự mâu thuẫn với các mục hàng 19–27 phía sau |
| B11 / B12 | thép chủ trên / dưới, nhiều nhóm `;` | ✅ | PAR:84–88 (vẽ nhóm đầu) |
| B12 (858) | > 11.7 m tự nối so le | ❌ | thanh liền |
| gối h11 | bề rộng cột dưới; 0 = không cột; âm = neo vách | ✅ / ⚠️ (âm) | thiếu `bxh` = gối dầm (DY7 luật 2, PAR:188) |
| nhịp h11 | L hoặc Ln | ✅ | HPRebar: L thông thủy (RT S1) |
| h12 C… | đường dim; nhịp = ghi đè h sàn | ⚠️ [P] | chỉ trích sách; parser không đọc |
| h13 gối | lớp 1, cùng cao độ thép chủ, xen giữa | ✅ | KataSupportTopBarLayout doc-comment |
| h13–15 gối `trái;phải`, `x;0` | ✅ | PAR:197–203 ParseSides; template K14, E15 khớp |
| h13–15 `-` | "chạy suốt qua gối" | ❌ | `-` ở gối = thanh cùng hàng của gối kề kéo tới gối này rồi neo/cắt; chuỗi `-` nối tiếp (DY7 luật 4, DY14 K16) |
| h13–15 ô nhịp `-` | thép chạy suốt nhịp | ⚠️ [P] | parser bỏ qua ô nhịp hàng 13–16 |
| h13–15 cắt | Ln/3 + 500, −h mỗi lớp | ❌ | H5·L bên mình, ngoài +G1 |
| h14/h15 thẳng hàng đứng với lớp 1 | ⚠️ | code lớp dưới trải đều, thanh lớn ra mép |
| h16 | gia cường **dưới** lớp 3 (ô nhịp), ô gối trống | ⚠️ [P] | PAR:196 đọc C16/E16… = gia cường **trên** lớp 4, bỏ qua ô nhịp. XLSM: B15 = B16 = "lớp 3", merge chỉ A13:A14 và A17:A18, nhóm outline 15–16 → nhãn ủng hộ cách hiểu của tài liệu (đối xứng 13/14/15 ↔ 16/17/18). Cần kiểm bằng một bản vẽ Kata có hàng 16 |
| h17 nhịp | dưới lớp 2, nằm trên lớp 1 | ✅ | PAR:243–247 (hàng 17 = lớp 2) |
| h17 cắt 0.15Ln/0.7Ln; `-` chạy suốt (J17) | ❌ / ⚠️ | cắt min(H3·L, L/6); `-` ở nhịp chưa đo |
| h17 gối (I17 = 2f20) | thép đáy qua nút dầm phụ | ⚠️ [P] | parser bỏ qua hàng 17/18 ở ô gối |
| h18 nhịp | lớp 1 cùng cao độ thép chủ, xen giữa | ✅ | KataSpanBottomBarLayout; cắt "0.15Ln, neo 15d" ❌ (hàng 17 − G1) |
| h19 gối | cột trên `b;lệch` | ✅ | PAR:205 |
| h19 nhịp | `ht;nfd` giật mép trên (+ lên), đổi thép trên | ✅ (comment F19/H19) | bẻ "giò gà" ⚠️ |
| h20 gối | `b x h` dầm giao (vd C20 400x500, K20 400x800) | ❌ | Kata.xlsm C20 = K20 = 400 (ví dụ bịa); PAR:206 đọc số |
| h20 nhịp | cốt giá cục bộ `nfd`, 0 = không | ✅ | K15 n lớp × 2; comment F20/H20 |
| h20 nhịp số thuần = đổi bề rộng (L20 = 300) | ⚠️ [P] | parser coi hàng 20 nhịp chỉ là cốt giá |
| h21 gối | lệch dầm giao, ≤ cột/2 | ✅ | PAR:207; comment A21 |
| h21 nhịp | bậc đáy `Δ;nfd` (+ lên = nông hơn) | ✅ | PAR:250 + depth = B5 − bậc (DY7T `Span_depths_come_from_B5_minus_row_21`) |
| h22 gối / nhịp | tên trục / đai riêng a1/a2/a3 | ✅ | PAR:208, 252–264; vùng "2h" ❌; cú pháp `d8a100/200` ⚠️ [P] (ParseStirrupSpacing không hiểu tiền tố Ø → rơi về G7) |
| h23 gối / nhịp | lệch trục so với tâm cột / bước đai gia cường cục bộ | ✅ | PAR:209; nhịp chỉ ghi chú. "trống → theo hàng 22" ⚠️ (Kata có radio I8/J7) |
| h24 | C24 `30` đai chống xoắn; `*` bỏ đai gia cường nút | ✅ (comment) | ý nghĩa `lap_xoan` = 30·ds ⚠️; parser chỉ ghi chú |
| h25–27 | 3 "lớp" đai phụ, A25/A26/A27 = nhãn gợi ý từng lớp | ❌ | cặp (lẻ = loại, chẵn = thanh) theo nhịp đúng ✅, nhưng vùng là **25–44** (DV C25:C44…), A25:A27 là **danh sách nguồn dropdown** (`Đai □`,`Đai U`,`Đai C`), không phải nhãn hàng; tự mâu thuẫn L1325 (25 = C, 26 = U, 27 = □) |
| C25:D25 … N25, C26:D26, C27:D27 | ví dụ mẫu | ✅ | XLSM khớp (U 3-4, C 2, C 5, M25 C/N25 2) |
| L1654–1666 | cột ThepDamAllMBKC | ⚠️ | khối mất tiêu đề, dính vào hàng 27 |

Mâu thuẫn parser cần xử lý (không thuộc báo cáo này để sửa): **h16** (khả năng parser sai), **B7** cú pháp sàn, **h22** tiền tố Ø, **h20** số thuần, ô nhịp `-` hàng 13–17, ô gối hàng 17/18. **I8**: parser đúng, tài liệu sai.

## 3. Khẳng định "Kata_Class_Lib / struct info_* / C#" — cần đối chiếu code dịch ngược

Đếm: 43 khối ```csharp; 233 dòng chứa định danh code (`Kata_Class_Lib`, `info_*`, `Pub_Sub`, `Pub_Function`, `R02_BeamsRebar`, `*Model.cs`, `C#`); 14 lần nhắc add-in `R02_BeamsRebar`. Lưu ý: P1 dòng 16 ghi chuỗi DLL Kata bị mã hoá; RT: Kata nạp/lưu bằng VBA `save_info` — các đoạn `ws.Cells[r,c]` kiểu C# là tái dựng, không phải trích nguyên văn. Không đánh giá đúng/sai; cột cuối chỉ ghi chỗ đã bị bằng chứng hành vi bác.

| Chủ đề | Dòng | Bị bằng chứng hành vi bác? |
|---|---|---|
| `info_vedam` đọc ô (ten_dam, so_ck, phi_do_gia, h[], phi_ke, ten_truc, lt_dam, ct_dam, a_dai, vòng `nhip`) | 238–241, 337–340, 382–386, 413–419, 543, 549–552, 630, 657–666, 685, 731–734, 769, 817–830 | vòng `nhip` dừng ở nhịp = 0 ≠ hành vi save/load (ô hàng 11 trống đầu tiên) |
| `info_lap_anchor` (G2/G3 → lap/anchor) | 195–208, 297–302 | — |
| Thuật toán neo Lx/Ly ceil 25 | 209–223 | khớp DY7 (chân = phần thiếu, tròn 25) |
| Cắt gối `L_cat_lop1/2` (+G1, −h) | 122–128, 482–492 | ❌ bị DY7 bác |
| Cắt bụng `x_cut` H3 | 315–320 | ❌ thiếu trần L/6, thiếu −G1 hàng 18 |
| `info_thep_gc_goi` / `info_thep_gc_nhip` / `thep_goi[,]` / `thep_nhip[,]` / `chuyen_thep_goi/nhip` | 303–314, 928–943, 974, 1009–1011, 1041–1054, 1081–1083, 1110–1112 | — |
| `info_thep_gia`, `chuyen_cot_gia` (âm → thep_ngang 0) | 367–381, 463–481 | khớp hành vi G5 |
| `info_tai_dam(1)`, `chuyen_h_san` | 572, 593–614, 888–901 | — |
| `chuyen_a_bv`, `chuyen_dai1` | 735–746 | — |
| `Pub_Function.cao_trinh` | 770–771 | — |
| `info_cot_dam`, `info_tietdien`, `chuyen_ht` | 1145–1169, 1203 | — |
| `info_dam_giao_taicot` (b, h, lt) | 1175, 1187, 1196 | ví dụ `bxh` hàng 20 sai với template |
| `info_truc` | 1227–1236, 1307–1316 | — |
| `info_dai_gccl`, `info_mc`, `chuyen_dai3` | 1261–1280, 1339–1358 | — |
| `U_ngoai`/`lap_xoan`, `info_BlockBeam_ThepBoTri`, cờ `*` | 1402–1423 | — |
| `info_dai_gc_input`, `tach_vtr_thep` | 1475–1514, 1558–1574, 1636–1650 | — |
| `info_SecondBeam`, tab Thép mặc định, `daiGCDamGiaoTaiGoiCb` | 1910–1964 | — |
| `info_giat_WC`, `InfoBlockOng`, `info_cog_hook` | 1968, 2229, 2279–2286 | — |
| Ánh xạ cột ThepDamAllMBKC | 129, 1281–1286, 1359–1365, 1424–1425, 1515–1520, 1575–1576, 1651–1666 | — |
| Add-in `R02_BeamsRebar` (SettingModel, SideBarModel, StirrupsViewModel, AddTop/BottomBarViewModel, SpecialBarModel, InfoModel) | 224–226, 321–324, 387–402, 493–496, 548–556, 615–617, 667–670, 747–750, 831–833, 2063–2093 | ➖ không phải HPRebar |

## 4. Tổng kết

**Chất lượng chung: thấp cho mục đích làm luật thép.** Phần mô tả ô (nhãn, comment, giá trị mẫu) phần lớn khớp Kata.xlsm thật; phần **luật hình học** (cắt, neo, vùng đai, giật cấp, cốt giá, nối) phần lớn sai so với bản vẽ Kata đo được, và phần "TCVN" trộn TCVN 356/5574:1991, ACI, EC8 với số mục không nhất quán. Bằng chứng mạnh nhất: phase 1 (P1) đã hiện thực các số của chính tài liệu này và DY7 buộc lật lại 4/6 số (đai dày, cắt bụng, chân móc, chia 11.7 m).

**Top 10 lỗi gây sai thép**
| # | Lỗi (dòng) | Đúng |
|---|---|---|
| 1 | Thép gối cắt Ln/3 (+500), lớp 2 = lớp 1 − h (115–118, 450–455, 912, 972, 1008, 1713–1715, 2044–2045) | mọi hàng H5·L (ceil 50, gốc I5) của nhịp bên đó; hàng ngoài ≥ hàng liền trong + G1 |
| 2 | G1 = đoạn w TCVN cộng vào thanh (57, 108–110, 488, 2043) | G1 = bước lệch giữa các lớp; thanh 1 lớp không +G1; ô G1 dương ghi đè setting 500 |
| 3 | Gối giữa dùng max(Ln1, Ln2) (1716) | mỗi bên theo nhịp của bên đó |
| 4 | Gia cường bụng cắt 0.15–0.2Ln, dài 0.6–0.7Ln (288–291, 1036, 1070, 1103, 1729–1732) | hàng 17 dừng min(H3·L theo I3 ceil 50, L/6 tròn gần nhất 50); hàng 18 = hàng 17 − G1 |
| 5 | Vùng đai dày 2h (1247, 1792, 1803, 2049, 2114) | ceil50(0.25·L0) từ mép, đai đầu 50, vùng giữa chia đều ≤ bước |
| 6 | Chân neo ≥ 15d–30d, dưới thẳng ≥ 20d (1770–1773) | chân = đúng phần thiếu của G2·d / G3·d (đo từ mép trong, thanh tới mép xa − a), min 0, ceil 25 khi vừa |
| 7 | Chia thanh 11.7 m, vùng nối, so le 50% (858, 2013–2021, 2088–2093) | thép chủ vẽ liền (Kata DY7 vẽ 16.2 m một thanh; quyết định user) |
| 8 | Radio I8 đảo ngược (589–590, 650–653, 670) | I8 = 1 giống đai ngoài; I8 = 2 (mặc định) đều J7 = a500 |
| 9 | Giật cấp: ≤ 100 uốn Z, > 100 cắt + neo 40d (1985–1997, 2061, 2120–2121) | bậc đáy: uốn 1:6 khi Ø ≥ 16 và (bậc − Ø)/rộng gối ≤ 1/6; cắt: nhịp sâu tới mặt xa bẻ lên G3·d, nhịp nông thẳng G3·d |
| 10 | Nghĩa ô hàng 12/21/23/24/25–28 sai (798–816), `bxh` đặt ở hàng 20 (1177) | theo §2; `bxh` ở **hàng 11** gối = gối là dầm |

Thêm (ít nghiêm trọng hơn): móc C cốt giá d8a400 (1870) → Ø đai @J7; cốt giá từng nhịp (356) → liên tục qua gối giữa; α = 1.2 cho neo (169) là hệ số nối; b_đai = b − 2c + 2ds (529, 722); J9 một số (728); I3 mặc định (281); hàng 16 ⚠️ (§2).

**Trùng lặp / dài dòng**
- Mục 3.2 chiếm ~1580/2290 dòng (69 %); 3 khối hàng 25/26/27 gần giống nhau (~240 dòng).
- G1/I1/cắt lệch giải thích ≥ 8 lần (57, 102–128, 444–457, 912, 926, 970, 1005, 1718); neo 40d/30d ≥ 4 lần (147–226, 242–276, 1736–1779, 2034); vùng đai ≥ 6 lần (573–585, 631–648, 1237–1260, 1785–1813, 2049, 2114); cốt giá ≥ 5 lần.
- Văn quảng cáo không mang thông tin ("hoàn hảo", "tuyệt đối", "triệt tiêu hoàn toàn", "100% file mẫu"); thông tin tác giả/URL chiếm cả khối ô.

**Vấn đề cấu trúc**
- Luật nằm rải trong catalogue ô và lặp lại ở Ch.4–12 với số khác nhau; không có mã luật, không có cấp nguồn (Kata đo / TCVN / thói quen / suy đoán).
- Tự mâu thuẫn: checkbox I1 vs "H2" (111 vs 927, 971); thép kê H6 vs "I6" (532 vs 950); "H4" không tồn tại (1036, 1070); khe lớp 25 vs 30 (538, 2249 vs 1708); móc 7.5d vs max(10d,75) (2273 vs 1818); cốt giá ≤ 400 vs min(h/2,400) (348 vs 1860); hàng 25/26/27 = U/C/C (814) vs C/U/□ (1325) vs cặp tự do (1431); hàng 24 nhịp = lỗ MEP (813) vs trống (1400); số mục neo 10.3.5.1 (157) vs 10.3.2 (2034); gối giữa Ln trái/phải riêng (913) vs max (1716).
- Đoạn ThepDamAllMBKC (1654–1666) mất tiêu đề, dính vào hàng 27; tiêu đề "Dòng 10 đến 28" nhưng hàng 28 chỉ có "1".
- Code "dịch ngược" trình bày như sự thật dù DLL bị mã hoá; trộn với mapping của add-in khác (`R02_BeamsRebar`).
- Dòng kết "đã kiểm chứng" (2289) không có cơ sở.

**Status:** DONE_WITH_CONCERNS
**Summary:** Tài liệu đúng phần lớn về nhãn/comment/giá trị mẫu của sheet Dam nhưng sai các luật hình học cốt lõi (cắt gối/bụng, G1, vùng đai, neo, giật cấp, 11.7 m, I8) so với bản vẽ Kata đo được; TCVN trích lẫn tiêu chuẩn cũ.
Concern: hàng 16 — nhãn template ủng hộ "gia cường dưới lớp 3", parser đọc "gia cường trên lớp 4"; cần một bản vẽ Kata có hàng 16 để chốt.
