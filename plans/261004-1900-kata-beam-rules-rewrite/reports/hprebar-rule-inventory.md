# HPRebar Kata beam-rebar rules — inventory of what the code implements (2026-10-04)

Read-only scan. No code changed. Line numbers checked against working tree (branch RebarVersion1, HEAD 8eb245e).

**Path legend**
- `C/` = HPRebar/HPRebar.Core/KataRebar/ (Calculators/, Models/, Parsers/)
- `X/` = HPRebar/HPRebar.Core/KataExport/Calculators/
- `T/` = HPRebar/HPRebar.Core.Tests/KataRebar/ (test name = method, :line = declaration)
- `TX/` = HPRebar/HPRebar.Core.Tests/KataExport/
- `R/` = HPRebar/HPRebar/KataRebar/Service/ (Revit side, only where it changes the rule)

**Verification legend** (column "Verified")
- **DY7** = matches T2-DY7.dwg (unit test tolerance ±25 mm + live Revit): plans/261002-0930-kata-dy7-match/reports/live-verify-dy7-dy14.md
- **DY14** = matches the T2-DY14 frame / E500 variant in T2-DY7.dwg: plans/261002-1420-kata-dy14-match/reports/live-verify-dy14.md (E500 variant = unit test only)
- **TIES** = DWG evidence table in plans/261002-1130-kata-layer-spacer-ties/plan.md + reports/live-verify-dy7.md
- **SEC** = section drawing only (not 3D model): plans/261003-1900-kata-section-cad-drawing/reports/dwg-section-rules.md
- **P1** = live Revit vs spec numbers, NOT vs Kata: plans/261001-0900-kata-beam-rules-phase1/reports/phase-06-live-verify.md
- **MVP** = HPRebar own rule, never checked against Kata: plans/260928-1259-kata-rebar-mvp/reports/rule-table.md
- **—** = unit test only, no Kata evidence

Note: docs/specs/kata-beam-rebar-rules.md §1 rows R3 (2h), R4 (0.15 Ln), R5 (15d), "H3 for rows 14-16", "móc C a400" are stale — superseded by K6/K7/K8/K12 in §0 and by the code below.

---

## 1. Cover / J9 (a/b)

| Rule | Formula / default | Cell / setting | Code | Tests | Verified |
|---|---|---|---|---|---|
| J9 parse "a/b": a = face → main bar centre, b = clear cover to stirrup outer face; missing/≤0 part → 0 | "50/25" → (50,25) | J9 | C/Parsers/KataBarNotationParser.cs:232-241; KataDamSheetParser.cs:56 | T/KataStressAdversarialTests.ParseCover_AdversarialInputs_ReturnsGivenNumbersOrZero:164 | MVP |
| J9 "a" only → b = a − max(dTop,dBot)/2 − ds; b < 15 warn; b ≤ 0 error (blocks) | thin 15 | J9 | C/Calculators/KataDetailingRuleBuilder.cs:40-48 | T/KataDetailingRuleBuilderTests.A_single_number_puts_the_stirrup_against_the_main_bars:33, _leaving_a_thin_stirrup_cover_warns:45, _leaving_no_room_for_the_stirrup_blocks:54 | MVP, P1 (J9 40) |
| J9 empty → b = 25, bars rest on stirrup | 25 | J9 | KataDetailingRuleBuilder.cs:18, 49-52 | An_empty_cell_uses_a_25_mm_stirrup_cover_and_bars_resting_on_it:62 | MVP |
| Bar centre depth = a, raised to resting b + ds + d/2 when a would cut the stirrup (+warning) | DY7 J9 30/25, Ø8, Ø18 → 42 | J9, G6, B11/B12 | KataDetailingRuleBuilder.cs:54-55, 119-131 | A_bar_centre_inside_the_stirrup_is_raised_and_reported:72 | DY7: accepted Δ12 (Kata draws 30/38, HPRebar 42) |
| G6 empty → parser default Ø10 (draws Ø10 hoops); G6 ≤ 0 → no stirrups, mains still reserve Ø10 + warning | ?? 10 | G6 | KataDamSheetParser.cs:59; KataDetailingRuleBuilder.cs:19, 32-34; KataStirrupZoneLayout.cs:29-30 | — | — ⚠️ empty G6 ≠ "no stirrups" |
| Two main layers must fit the shallowest span: topDepth + botDepth < depth else error | — | B5, row 21 | KataDetailingRuleBuilder.cs:61-63 | Two_layers_deeper_than_the_beam_block:90 | — |
| Transverse: outer bars touch hoop inner face y = ±(b/2 − c − ds − d/2), others evenly between; 1 bar on centre line; overcrowded → all at 0 | — | B6, J9, G6 | C/Calculators/KataRebarCalculator.cs:22-45; KataDetailingRules.cs:149-150 (EdgeBarOffset) | T/KataRebarCalculatorTests.Calculate_TransverseYCentering_GuaranteesSymmetryAcrossWidth:713; KataStressAdversarialTests.ComputeTransverseYPositions_*:599/621 | DY7 |
| Section DRAWING (not model): hoop centreline at c, outer bars at c + (d+ds)/2 (38, model 42) | — | — | C/Calculators/KataSectionBars.cs:18-24, 84 | T/KataSectionDrawingTests.T2_DY7_section_is_drawn_as_Kata_draws_it:21 | SEC |

## 2. Main bars B11 / B12 and extent

| Rule | Formula / default | Cell | Code | Tests | Verified |
|---|---|---|---|---|---|
| Only first group of B11/B12 drawn; 2nd+ groups (";") skipped + reported | — | B11, B12 | C/Calculators/KataScopeFilter.cs:41-42, 59-62; KataDamSheetParser.cs:85-88 | T/KataScopeFilterTests.Only_the_first_main_bar_group_is_drawn:41 | — |
| Top mains: one bar, level, from end support 0 to last, anchored both ends (§3) | z = −TopBarCentreDepth | B11, G2 | C/Calculators/KataMainBarLayout.cs:33-43 | T/KataRebarPlannerTests.Top_bars_sit_43_below_the_top_and_hook_down_450_at_both_columns:33 | DY7 (42→16208 vs Kata 30→16215), DY14 |
| Bottom mains: follow each span's soffit; consecutive spans share one bar (crank) or cut at a step (§10) | z = −depth(s) + BottomBarCentreDepth | B12, G3, row 21 | C/Calculators/KataBottomMainBarRuns.cs:40-119; KataMainBarLayout.cs:47-58 | T/KataDy7DrawingTests.Bottom_main_bars_crank_over_the_100_step_and_split_at_the_250_step:114 | DY7, DY14 |
| Each bar one piece however long (no stock split, no laps) | — | — | KataMainBarLayout.cs:14-15 (doc) | T/KataMultiSpanPlanTests.A_main_bar_longer_than_a_stock_bar_is_drawn_whole_and_not_reported:26; KataRebarCalculatorTests.Calculate_MainBarsLongerThanAStockBar_StayWholeWithoutAWarning:786 | user decision 2026-10-01 |
| Shape codes 00 / 05a / 15a; DimR = 2d (metadata only); marks "1", "2" | — | — | KataMainBarLayout.cs:194-200; KataBottomMainBarRuns.cs:164-174 | T/KataShapeCodeAndBarMarkTests.Calculate_AssignsStandardKataShapeCodesAndBarMarks:13 | — |
| Cantilever tip (top): stops at stirrup cover, hook down min(room, max(G3·d, 200)); bottom stops at column face. **Unreachable via planner** (scope filter blocks width-0 supports) | 200 min | G3 | KataMainBarLayout.cs:88-96, 109-110 | KataRebarCalculatorTests.Calculate_CantileverOverhangBeam_*:357/569/621 (calculator direct) | — |

## 3. Anchorage at end supports (G2 / G3)

| Rule | Formula / default | Cell / setting | Code | Tests | Verified |
|---|---|---|---|---|---|
| Required length: top G2·d (tension), bottom G3·d (compression); empty cell → 40 / 30 | 40 / 30 | G2, G3 | KataDamSheetParser.cs:41-42; KataDetailingRuleBuilder.cs:21-22, 88-89 | T/KataDetailingRuleBuilderTests.Anchorage_factors_come_from_G2_and_G3:81 | MVP |
| Straight when available = supportWidth − a − inset ≥ required; end at innerFace ± required | — | — | C/Calculators/KataAnchorage.cs:47-49 | T/KataAnchorageTests.A_wide_support_holds_the_bar_straight:10 | MVP |
| Else: horizontal end at outer face − a (− inset), 90° leg = max(required − available, MinimumLegFactor·d) | MinimumLegFactor 0 (was 15) | settings | KataAnchorage.cs:51, 65-66; KataSettings.cs:59 | A_narrow_support_bends_the_bar_at_the_far_face:20; The_leg_is_never_shorter_than_the_minimum:32; T/KataSpecRuleTests.A_leg_bends_exactly_the_missing_length_by_default:74 | DY7 (bottom Ø18 col 450: leg 125) |
| Horizontal end cover = vertical centre depth "a" (same value both directions) | — | J9 | KataMainBarLayout.cs:156-160; KataAnchorage.cs:66 | — | DY7 Δ12 accepted |
| Leg rounded UP to multiple of RoundLegMm when it still fits, else exact | 25 | RoundLegMm | KataAnchorage.cs:59-63; KataSettings.cs:65 | KataSpecRuleTests.A_leg_is_rounded_up_to_25_when_the_rounded_leg_fits:56, A_rounded_leg_that_would_not_fit_keeps_its_exact_length:65 | P1 (470→475) |
| Leg clamped to room = SupportDepth − topCentre − botCentre; shortfall > 0.5 → warning | — | B5/row 21/"bxh" | KataAnchorage.cs:53-58; KataMainBarLayout.cs:79-80, 165-169 | A_leg_longer_than_the_beam_allows_is_clamped_and_reported:40 | DY7 ⚠️ Kata draws legs 300/400 past beam face; HPRebar clamps (266) |
| Bottom-leg inset (A5): when top leg + bottom leg > room, bottom leg moves inboard by level.Inset + (dTop+dBot)/2 + max(25, dBot); 2 passes | MinimumLegGap 25 (const, not a setting) | — | KataAnchorage.cs:73-78; KataMainBarLayout.cs:99-136 (loop 124-133); KataDetailingRules.cs:81 | T/KataAnchorageTests.A_bottom_leg_moved_inboard_keeps_its_anchorage:49, Legs_overlap_only_when_*:60; T/KataSupportTopBarTests.Bottom_legs_move_inboard_of_the_innermost_top_leg:68 | P1; DY7 ⚠️ HPRebar 133 vs Kata 35 (HPRebar convention, accepted) |
| Additional top bars in end support anchor like mains (G2·d), bend inboard by level inset, leg room = level.Z − bottom centre | — | G2 | C/Calculators/KataSupportTopBarLayout.cs:143-161 | T/KataSupportTopBarTests.Row_13_bars_fill_the_gaps_of_the_main_level_and_anchor_like_the_main_bars:30, Row_14_bars_sit_one_layer_down_with_their_bend_inboard:49 | DY7 |
| Crossing-beam support ("bxh"): leg room limited by min(span depth, beam h) | — | row 11 "bxh" | C/Models/KataBeamRebarSpec.cs:116-124 | T/KataSteppedBeamEdgeTests.A_crossing_beam_support_limits_the_legs_to_its_own_depth:105 | DY7 (I 200x350, leg 266 clamped vs Kata 300) |

## 4. Bars through interior supports

| Rule | Code | Tests | Verified |
|---|---|---|---|
| Top mains run straight through every interior support (level top) | KataMainBarLayout.cs:36-43 | KataRebarCalculatorTests.Calculate_MultiSpanContinuousBeam_B01KataSample_*:186 | DY7 |
| Bottom mains continuous through interior supports with equal soffit; cranked/cut at steps (§10) | KataBottomMainBarRuns.cs:51-119 | KataDy7DrawingTests:114 | DY7, DY14 |
| Additional top at interior support: symmetric cell → one bar across; "left;right" asymmetric → each side its own slots; stronger side (Σ n·d²) anchors at far face of support (leg stops LayerGap above bottom bars), weaker runs through + G2·d into neighbour span (clamped to beam ends) | KataSupportTopBarLayout.cs:102-132, 150-158, 170-181 | KataSupportTopBarTests.An_interior_support_with_different_sides_draws_one_bar_per_side:92, The_side_with_more_steel_hooks_even_when_it_is_on_the_right:147, At_an_interior_support_the_hooked_leg_stops_a_layer_gap_above_the_bottom_bars:162, An_interior_support_with_equal_sides_draws_one_bar_across:188 | — (no DWG case) |
| Side bars of consecutive equal spans run through interior supports (§8) | KataSideBarLayout.cs:54-82 | KataDy7DrawingTests.Side_bars_run_on_through_E_and_stop_at_spans_with_none:134 | DY7 |
| Known: an interior-support cut may stop inside the neighbouring column when that span is short (only beam ends clamp) | KataSupportTopBarLayout.cs:76-79 | — | docs §3 "đã biết" |

## 5. Additional top bars rows 13-16

| Rule | Formula / default | Cell / setting | Code | Tests | Verified |
|---|---|---|---|---|---|
| Row 13 shares main level, fills gaps between main bars (extra bars to gaps nearest centre); <2 mains → spread over width | — | 13 | C/Calculators/KataLayerPositions.cs:16-39; KataSupportTopBarLayout.cs:233-235 | KataSupportTopBarTests.Gaps_take_extra_bars_nearest_the_centre_first:199 | DY7 |
| Rows 14-16 stack below: z = above.Z − (dAbove/2 + LayerGap + d/2); empty row takes no room; Inset = mainZ − z | LayerGap max(30, d) | 14-16, LayerClearGap | C/Calculators/KataTopLayerStack.cs:26-46 | KataStressAdversarialTests.Calculate_4LayersTopExtraBars_StrictZHierarchyAndNonOverlap:449; KataTopBarStaggerTests.An_empty_row_takes_no_step:55 | P1 (C14 z −96) |
| Rows 14-16 spread full width touching hoop | — | — | KataSupportTopBarLayout.cs:235 | — | DY7 |
| Cut (every row 13-16): reach = RoundUp(H5 × L_side) from face (or centre if I5 contains "tâm"); L_side = clear span on THAT side; empty/≤0 H5 → 0.25. H3/I3 NOT used for top | H5 0.25, round 50 | H5, I5, RoundCutExtraMm | KataSupportTopBarLayout.cs:194-203; KataDamSheetParser.cs:44-49 | T/KataTopBarStaggerTests.Every_row_reaches_H5_of_its_own_span_before_the_stagger:69; KataRebarCalculatorTests.Calculate_UnequalAdjacentSpans_InteriorSupportTopCutoffUsesEachSidesSpan:669; KataSupportTopBarTests.A_blank_I5_measures_from_the_support_face:274 | DY7 (C 1850/2350, E 4550·8200/4050·8700…) |
| Cut clamped to beam ends: ≥ SupportStart[0] + a, ≤ SupportEnd[last] − a | — | — | KataSupportTopBarLayout.cs:76-79 | — | — |
| G1 stagger: on each side, each filled outer row reaches ≥ RoundUp(innerReach + G1) past the next filled inner row (incl. run-through end of weaker side); only outer cut moves outward; limited to opposite support face / beam end − a; shortfall warned | G1 > 0 else CurtailedExtensionMm 500; 0 = off | G1, settings | C/Calculators/KataTopBarStagger.cs:40-110 (wanted :88); KataDetailingRuleBuilder.cs:65-67 | KataTopBarStaggerTests (all 11: Each_outer_row_reaches_500_further_than_the_row_inside_it:38 … An_outer_row_held_at_the_face_across_a_short_span_is_reported_with_what_it_got:187) | DY7, P1 (G1 700) |
| G1 read only as one positive number; other text → settings + warning | — | G1 | C/Parsers/KataBarNotationParser.cs:40-42; KataDamSheetParser.cs:39-40 | KataSpecRuleTests.G1_is_read_only_as_one_positive_number:128, An_empty_G1_uses_the_settings_without_a_warning:140; KataTopBarStaggerTests.A_G1_that_is_not_one_number_*:98 | P1 ("-300;11700") |
| "I1" — no code reads it | — | I1 | (grep: none) | — | not implemented |
| "-" in row 13 = neighbour support's row-13 bar continues; chains over consecutive "-"; at end support → anchor (cantilever tip → cover stop); interior → through + RoundUp(RoundUp(H5·L) + G1 if that support has rows 14-16) | — | row 13 "-" | C/Calculators/KataTopBarContinuation.cs:18-19, 25-57, 73-78 | KataDy7DrawingTests.Additional_top_bars_reach_H5_*:78 (G13→I); KataDy14DrawingTests.At_E_350_*:175 (G13→K); KataSteppedBeamEdgeTests.A_dash_beside_a_cantilever_tip_stops_the_bar_a_cover_short_of_it:72 | DY7, DY14 |
| "-" in rows 14-16 → warning, not drawn | — | 14-16 | KataSupportTopBarLayout.cs:58-59 | KataSteppedBeamEdgeTests.A_dash_in_row_14_is_reported_and_never_doubles_a_level:86 | — |
| "-" chain claimed from both sides → warning only (bars still doubled) | — | 13 | KataTopBarContinuation.cs:63-70; KataSupportTopBarLayout.cs:95-96 | KataDy14DrawingTests.A_dash_chain_between_two_supports_with_bars_is_reported:133 | open Kata semantics (DY14 review M1) |
| "left;right" split (';'); end support uses span side only, other side warned when span side empty; same bars in other order = symmetric | — | 13-16 | KataBarNotationParser.cs:97-114; KataSupportTopBarLayout.cs:68-72 | KataSupportTopBarTests.A_left_right_cell_over_an_end_support_uses_the_span_side:78, Sides_listing_the_same_bars_in_another_order_are_symmetric:287, An_end_support_cell_with_bars_only_outside_the_beam_is_reported:223 | — |
| Asymmetric slot sharing: PartitionInterleaved, stronger side outer slots | — | — | KataLayerPositions.cs:72-169 | PartitionInterleaved_splits_slots_symmetrically:120, Both_sides_of_an_interior_support_take_separate_slots_across_the_beam:174 | live (fixed-number report: E14 `2f16;2f14`) |
| In-layer spacing: warn clear < max(25, d); block overlap | BarClearSpacing 25 (const) | — | KataLayerPositions.cs:46-65; KataDetailingRules.cs:91-94 | Bars_too_close_in_a_layer_are_reported:212 | — |
| Layer depth check: each lower level must clear bottom mains by LayerGap → blocking | — | — | KataSupportTopBarLayout.cs:42-47 | More_layers_than_the_depth_holds_block:258 | — |
| Unreadable tokens warned, rest drawn; "0" = nothing | — | — | KataSupportTopBarLayout.cs:55-66 | An_unreadable_cell_is_reported:235, A_partly_unreadable_cell_is_reported:246 | — |
| Narrow "-" end support stacks rows 14-16 under main Ø even if chained bar thicker | — | — | KataTopLayerStack.cs:32 | — | open (DY14 review L4) |

## 6. Additional bottom bars rows 17-18

| Rule | Formula / default | Cell / setting | Code | Tests | Verified |
|---|---|---|---|---|---|
| Row 18 = layer 1 (main level, between bottom mains, larger bars at edges, seat = max(a, c+ds+d/2)); row 17 = layer 2 above, z = top of level + LayerGap + d/2, spread full width, larger at edges | LayerGap max(30, d) | 17, 18 | C/Calculators/KataSpanBottomBarLayout.cs:48, 71-97, 122-127 | T/KataSpanBottomBarTests.Row_18_bars_split_the_gaps_of_the_bottom_main_level:32, Row_17_bars_sit_one_layer_up_spread_over_the_width:54, Row_17_alone_takes_the_level_above_the_main_bars:65, A_row_18_bar_larger_than_the_main_bars_rests_on_the_stirrup:77, A_mixed_row_17_cell_puts_the_larger_bars_at_the_edges:149 | DY7 |
| Inner-row cut distance from each face = max(0, min(RoundUp(H3·L) [− width/2 if I3 "tâm"], RoundNearest(L/6))); empty/≤0 H3 → 0.20 | H3 0.20, cap 1/6 L, round 50 nearest (AwayFromZero) | H3, I3, BottomExtraCutFraction, RoundCutExtraMm | KataSpanBottomBarLayout.cs:107-117; KataDetailingRules.cs:63-68; KataSettings.cs:56 | KataDy7DrawingTests.Additional_bottom_bars_stop_at_the_smaller_of_H3_L_and_L_over_6_and_row_18_G1_nearer:98; KataDy14DrawingTests.The_variant_with_E_500_matches_the_drawing_bar_by_bar:149 | DY7, DY14 (K14 nearest); midpoint direction unpinned (review L1) |
| Row 18 under a filled row 17 = row 17 cut − G1 (closer to supports, never past faces); row 18 alone uses the cut directly | G1 / 500 | G1 | KataSpanBottomBarLayout.cs:59-64 | same DY7 test | DY7 (D18 850 / D17 1350) |
| Straight bars, no hooks; mark "4.s.l" | — | — | KataSpanBottomBarLayout.cs:204-223 | — | DY7 |
| Clear-of-top-bars check (top mains + support bars overlapping in x) → blocking | LayerGap | — | KataSpanBottomBarLayout.cs:99-100, 134-165 | Row_17_blocks_where_it_meets_support_bars_over_the_same_stretch:198, Row_17_passes_under_support_bars_that_stop_before_it_starts:207 | — |
| Cut points cross (xEnd − xStart < 1) → warning, not drawn; L ≤ 0 → warning | — | — | KataSpanBottomBarLayout.cs:52-56, 194-198 | A_span_without_length_draws_no_additional_bottom_bars:162 | — |
| No bottom mains → row 18 rests on stirrup, row 17 stacks on it | — | — | KataSpanBottomBarLayout.cs:48, 92 | Without_bottom_main_bars_row_18_rests_on_the_stirrup_and_row_17_stacks_on_it:94 | — |

## 7. Stirrup zones (G6 / G7 / G8 / row 22)

| Rule | Formula / default | Cell / setting | Code | Tests | Verified |
|---|---|---|---|---|---|
| Spacing parse: G7 → dense, G8 → middle, G9 → cantilever; "a150", "@150", "150"; empty → 150 / 200 / 150 | 150/200/150 | G7, G8, G9 | KataDamSheetParser.cs:59-64; KataBarNotationParser.cs:146-184; KataStirrupZoneLayout.cs:53-56 | KataStressAdversarialTests.ParseStirrupSpacing_AdversarialInputs_*:119 | MVP |
| Row 22 span override "a/b[/c]": dense, middle, right-zone (end) spacing; G7-only third part dropped by scope filter | — | row 22 (span) | KataDamSheetParser.cs:252-267; KataStirrupZoneLayout.cs:52-55; KataScopeFilter.cs:66 | T/KataInnerStirrupTests.Row_22_sets_the_span_spacings_with_its_own_last_zone:77; KataScopeFilterTests.Top_steps_are_skipped_soffit_steps_kept_and_span_stirrup_overrides_kept:60 | — |
| Dense zone length = min(Ln/2, Ceil50(max(DenseZoneHeightFactor·h_span, EndZoneFraction·Ln))) | factor 0, fraction 0.25, round 50 hard-coded | settings | KataDetailingRules.cs:136-137; C/Calculators/KataStirrupZoneLayout.cs:116, 189 | KataDy7DrawingTests.Dense_stirrup_zones_are_a_quarter_of_each_span:145; KataSpecRuleTests.With_a_height_factor_of_2_*:19 | DY7 (1400/1750/550) |
| First/last stirrup 50 from face | FirstStirrupOffset 50 (const) | — | KataDetailingRules.cs:125 | KataRebarPlannerTests.Stirrups_run_16_at_a100_then_14_at_200_then_16_at_a100:69 | DY7 |
| Dense zones: Even(face+50 → face+zone) at ≤ s (count = ceil(len/s), equal spacing) | — | — | KataStirrupZoneLayout.cs:130-131, 201-209 | KataSpecRuleTests.Stirrup_gaps_never_exceed_the_spacing_of_their_zone:198 | DY7 (500…1850) |
| Middle: gap ≥ 2.5·s → Even(lastDense + s → firstDense − s); 2·s ≤ gap → ceil(gap/s)−1 evenly; gap > s → one in middle; else none | — | G8 | KataStirrupZoneLayout.cs:162-178 | Stirrup_gaps_never_exceed_*:198; KataRebarCalculatorTests.Calculate_ShortSpanStirrupDistribution_ValidAndNonOverlapping:741 | DY7 (2050…4350) |
| Zones meet (Ln − 2·zone < min s): exact grid from each face; right stirrup < 0.5·s from last left dropped; gap > dense filled evenly ("Giữa nhịp (đai dày)") | — | — | KataStirrupZoneLayout.cs:119-127, 139-161 | KataSpecRuleTests.Dense_zones_that_fill_the_span_leave_no_middle_zone_and_no_stirrup_twice:38, Stirrups_of_a_short_span_never_stand_closer_than_half_their_spacing:243 | DY14 (250 span, numbering test) |
| Placed spacing ≤ nominal; NominalSpacing = sheet value → LabelSpacing on tags; Revit set uses placed spacing | — | — | KataStirrupZoneLayout.cs:77-78; C/Models/KataStirrupZoneResult.cs:33-36; R/KataStirrupSetCreator.cs:108 | — | DY7 tags |
| Hoop box per span: (b − 2c) × (h_span − 2c), centreline at c + ds/2 | — | J9, row 21 | KataStirrupZoneLayout.cs:43-50 | KataDy7DrawingTests:145 (OutToOutHeight 450/550/300) | DY7 |
| Zone merge for drawing: same hoop number + label spacing + gap ≤ 1.5 s | — | — | C/Calculators/KataStirrupRuns.cs:19-35 | KataBarNumberingTests.T2_DY14_*:47 | DY14 |
| Cantilever: one uniform zone at G9 from cover (blocked by scope filter) | — | G9 | KataStirrupZoneLayout.cs:58-61, 211-224 | calculator-direct cantilever tests | — |
| Row 23 (span "đai gia cường của nhịp"), row 24 (support "đai chống xoắn / gia cường tại gối") → skipped + reported | — | 23, 24 | KataDamSheetParser.cs:210, 269; KataScopeFilter.cs:53-54 | KataStirrupSectionParserTests.Row_24_markers_and_span_reinforcement_stirrups_become_notes:61 | not implemented |

## 8. Inner U / C / □ stirrups rows 25-44

| Rule | Code | Tests | Verified |
|---|---|---|---|
| Pair (support col = type, span col = bars "a-b"/"a"); entry only when bars cell filled; type by last char U / C / else closed | C/Parsers/KataStirrupSectionParser.cs:18-46 | T/KataStirrupSectionParserTests (all 6) | MVP (VBA `bieudo`) |
| Bars referenced by index into B11 top mains only; out of range → warning | C/Calculators/KataInnerStirrupLayout.cs:43-53 | KataInnerStirrupTests.Bars_the_section_does_not_have_are_reported:67 | — |
| "□ 1-n" = outer hoop → skipped | KataInnerStirrupLayout.cs:55-56 | A_closed_hoop_over_all_the_top_bars_is_the_outer_hoop:60 | — |
| □ / U legs at y(a) − (dTop+ds)/2 … y(b) + (dTop+ds)/2, top/bottom on hoop line; U open at top; hooks ClosedStirrupHookAngle/Factor 135°/7.5 | KataInnerStirrupLayout.cs:97-119 | A_U_stirrup_is_open_at_the_top_with_its_legs_outside_the_named_bars:23 | — |
| C a: upright tie beside bar a from top main centre to bottom main centre, 180°/7.5, wraps; "C a-b" a≠b → warning, uses a | KataInnerStirrupLayout.cs:58-59, 109-113 | A_C_tie_stands_beside_its_bar_with_180_degree_hooks:48; T/KataTieWrapTests.An_upright_C_tie_*:30 | — |
| Stations = outer hoop stations + ds, per zone (same spacing) | KataInnerStirrupLayout.cs:64-66 | Inner_stirrups_follow_the_outer_zones_one_stirrup_diameter_along:39 | — |

None of the inner-stirrup rules has Kata DWG evidence (DY7/DY14 have none).

## 9. Side bars (G4 / G5 / row 20)

| Rule | Formula / default | Cell / setting | Code | Tests | Verified |
|---|---|---|---|---|---|
| Global: \|G5\| layers of Ø G4, each layer 2 bars (one per face); G5 < 0 → layers kept, ties dropped | — | G4, G5 | KataDamSheetParser.cs:91-103, 179 | T/KataSideBarTests.A_negative_G5_keeps_the_layers_and_drops_the_ties:80 | DY7 |
| Row 20 per span overrides: "nfd" = n LAYERS × 2 bars (K15), d = max; "0" = none (no warning) | — | row 20 (span) | C/Calculators/KataSideBarLayout.cs:92-104; KataDamSheetParser.cs:249 | KataSideBarTests.Row_20_overrides_G5_for_its_span:89, Row_20_counts_layers_like_G5:98, A_zero_part_of_a_row_20_cell_is_ignored:56; KataRebarCalculatorTests.Calculate_SpanSideBarOverride_2f12_GivesTwoLayers:823; KataDy14DrawingTests.Row_20_2f12_draws_two_layers_*:204 | DY14 (z −386/−214 vs Kata −383/−217) |
| z: layers evenly between top-main centre and bottom-main centre (stirrup inner faces when no mains) of the SHALLOWEST span of the group: z = zBot + r·(zTop − zBot)/(n+1) | — | — | KataSideBarLayout.cs:36-39, 62-71 | G5_layers_spread_between_the_main_bars_and_anchor_10d_into_the_supports:21 | DY7 (z −250) |
| y = ±EdgeBarOffset (touch hoop) | — | — | KataSideBarLayout.cs:66 | — | DY7 |
| Consecutive spans with same (n, d) share one bar through interior supports | — | — | KataSideBarLayout.cs:54-60 | KataDy7DrawingTests.Side_bars_run_on_through_E_*:134 | DY7 (330 → 13520) |
| Anchorage: end support min(10d, width − a); group boundary at interior support min(10d, width/2 − d/2); width 0 → 0 | SideBarAnchorageFactor 10 | settings | KataSideBarLayout.cs:64-65, 107-112; KataSettings.cs:33 | Different_side_bars_of_two_spans_keep_to_their_half_of_a_narrow_interior_support:62, The_settings_set_the_anchorage_and_J7_the_tie_spacing:109 | DY7, DY14 |
| h_span ≥ 700 with no G4/G5 and row 20 empty → warning, nothing generated; 0 = off | SideBarRequiredHeight 700 | settings | KataSideBarLayout.cs:47-49, 84-86; KataSettings.cs:68 | KataSpecRuleTests.A_deep_beam_without_side_bars_is_reported_and_none_are_drawn:91, A_zero_height_limit_*:111 | — (TCVN, not Kata) |
| No layer-count sanity (10f12 → 10 layers) | — | row 20 | KataSideBarLayout.cs:64-71 | — | open (DY14 review L7) |
| Section DRAWING spreads side bars between inner faces of top/bottom bars (drawn −217/−383, real −214/−386) | — | — | KataSectionBars.cs:96-117 | KataSectionDrawingTests.T2_DY14_section_is_drawn_as_Kata_draws_it:26 | SEC |

## 10. C ties (side-bar ties, layer spacer ties)

| Rule | Formula / default | Cell / setting | Code | Tests | Verified |
|---|---|---|---|---|---|
| Side-bar ties: per span, per layer, across the two side bars, Ø = stirrup, hooks CrossTieHookAngle/Factor | 180° / 7.5 | G6, settings | KataSideBarLayout.cs:76-78, 118-147 | KataSideBarTests.Each_layer_gets_C_ties_at_J7_with_180_degree_hooks:36 | TIES (14 Ø8a500) |
| Side-bar tie straight part BELOW the side bars (WrapOffset −Z) | — | — | KataSideBarLayout.cs:139; C/Calculators/KataTieWrap.cs:17-36 | KataTieWrapTests.A_side_bar_tie_runs_one_bend_radius_under_the_bars_and_reaches_past_them:11 | ⚠️ SEC says Kata draws side tie straight ABOVE, tails below |
| Layer spacer ties: under rows 14-16 (top layer ≥ 2) and row 17; wherever a section holds ≥ LayerTieMinBarCount bars of that layer; hooks round the 2 outer bars of the section; straight part below layer | N = 3 (min 2) | LayerTieMinBarCount | C/Calculators/KataLayerSpacerTieLayout.cs:37-68, 79-108 | T/KataLayerSpacerTieTests.Every_second_layer_of_three_bars_in_T2_DY7_gets_ties_at_J7_span_by_span:24, The_tie_wraps_the_two_outer_bars_*:53, A_layer_of_two_bars_gets_no_tie_unless_the_setting_asks_for_two:75, Each_half_of_a_left_right_cell_is_counted_on_its_own_side:129 | TIES (live run 2 Δ ≤ 1) |
| Ties kept clearance = 50 + 2·ds inside bar ends and support faces; per span; never in supports | — | — | C/Calculators/KataTieStations.cs:20; KataLayerSpacerTieLayout.cs:43-46 | — | TIES |
| Spacing: I8 = 1 → beside every outer hoop (+2·ds along beam, hoop zone spacing); else uniform J7 from span face + clearance, count floor(len/J7)+1 (not centred) | J7 sensible 100–1000 else 500 + note (only when a tie is drawn) | I8, J7 | KataTieStations.cs:32-59; KataDamSheetParser.cs:67-68, 292-299; KataDetailingRuleBuilder.cs:70-80; KataRebarCalculator.cs:90-91 | KataDamSheetParserTests.Parse_J7_and_the_option_group_in_I8_give_the_tie_spacing:245; KataLayerSpacerTieTests.Like_hoops_*:85, An_unreadable_J7_*:118, A_J7_far_outside_*:142, Side_bar_ties_follow_the_same_option_group:101 | TIES (I8 2 live; I8 1 unit only) |
| Tie touching layer below (≈ 2.5·ds under centres) → warning | — | — | KataLayerSpacerTieLayout.cs:139-161 | A_tie_that_would_touch_the_layer_below_is_reported:162 | — |
| Tie knows wrapped bar Ø; Revit warns when bend radius < (Øbar + Øtie)/2 | — | — | KataLayerSpacerTieLayout.cs:59; R/KataRebarSectionFit.cs | A_tie_knows_the_diameter_of_the_bars_it_wraps:153 | TIES |

## 11. Layer clear gap, bar position

| Rule | Value | Code | Tests | Verified |
|---|---|---|---|---|
| MODEL layer gap (clear) = max(LayerClearGap, larger Ø) | 30 | KataDetailingRules.cs:87-88; KataSettings.cs:62 | KataSupportTopBarTests, KataSpanBottomBarTests | P1, TIES (layer 2 = 3550 − 90) |
| DRAWING layer gap (section) = 25 clear | 25 | C/Calculators/KataSectionStyle.cs:16; KataSectionBars.cs:88 | KataSectionDrawingTests | SEC (DY7 layer 2 −81 with layer 1 −38 for Ø18) ⚠️ model 30 vs Kata 25 — undecided |
| MODEL main centre = a or b + ds + d/2; DRAWING = c + (d+ds)/2 | 42 vs 38 (Ø18/Ø8/25) | KataDetailingRuleBuilder.cs:119-131; KataSectionBars.cs:84 | — | DY7 accepted Δ12; SEC |
| In-layer bar gap max(25, d) | 25 | KataDetailingRules.cs:91-94 | Bars_too_close_in_a_layer_are_reported | — |

## 12. Bar numbering

| Rule | Code | Tests | Verified |
|---|---|---|---|
| Order: top mains, bottom runs L→R, extra top support by support (row 13→16), extra bottom span by span, side bars, every C tie, inner stirrups, hoops span by span | C/Calculators/KataBarNumbering.cs:43-64 | T/KataBarNumberingTests.Every_bar_set_zone_and_single_stirrup_is_numbered_*:121 | DY7/DY14 tags |
| Identical bar (Ø, point count, hook angles, all coords + hook lengths within ±1 mm, incl. mirror / reversed views) shares number | KataBarNumbering.cs:84, 99-117 | Bars_within_a_millimetre_share_a_number:108, Mirror_image_bent_bars_*:92, T2_DY7_takes_Kata_s_numbers_and_E13_shares_6_with_the_identical_F17:30 | DY7 |
| All C ties of one Ø share one number; hoops numbered by type + W×H; single stirrups take first zone number of their span | KataBarNumbering.cs:51-64 | T2_DY14_takes_Kata_s_numbers_and_span_4_shares_the_hoop_19_of_span_3:47 | DY14 |

## 13. Stepped soffit (row 21), span depth

| Rule | Formula / default | Cell / setting | Code | Tests | Verified |
|---|---|---|---|---|---|
| Span depth h_i = B5 − row 21 (top level); B5 − row21 ≤ 0 → error | — | B5, row 21 | KataDamSheetParser.cs:147; KataBeamRebarSpec.cs:109-110; KataDetailingRuleBuilder.cs:57-58 | KataDy7DrawingTests.Span_depths_come_from_B5_minus_row_21:69 | DY7 |
| Support depth = own span (end) / shallower of two (interior) / ≤ crossing beam h | — | — | KataBeamRebarSpec.cs:116-124 | KataSteppedBeamEdgeTests:105 | DY7 |
| Crank when Ø ≥ CrankMinDiameter AND width > 0 AND (\|step\| − Ø)/width ≤ 1/6 (K13; replaced "step ≤ 100") | 16, slope 6 | CrankMinDiameter (setting), CrankSlope (const) | KataDetailingRules.cs:113-122 | KataDy14DrawingTests.At_E_350_*_cut_*:71, At_E_500_the_same_step_is_cranked:84, Only_bars_of_16_and_up_are_cranked:103, The_crank_diameter_comes_from_the_settings:112 | DY14 (82/350 cut, 82/500 crank), DY7 (82/500 crank) |
| Crank geometry: run 6·\|step\| from the shallower span's support face into the deeper span | — | — | KataBottomMainBarRuns.cs:122-127 | KataDy7DrawingTests:114 (5950→6550) | DY7 |
| Crank without room (overlaps previous crank, past span end, beside cantilever) → cut + warning | — | — | KataBottomMainBarRuns.cs:69-76 | KataSteppedBeamEdgeTests.Two_cranks_that_would_overlap_cut_the_bar_*:43 | — |
| Cut: deep bar to far face − a, bent up, leg ≤ min(step − Ø − LayerGap, under top bars); shallow bar straight G3·d past its face (clamped to beam end − a) | — | G3 | KataBottomMainBarRuns.cs:91-100, 133-138 | KataSteppedBeamEdgeTests.The_deep_bar_at_a_cut_step_keeps_its_leg_under_the_shallow_bar:119 | DY7 (13815 / 13310), DY14 |
| Cut with \|step\| − Ø < LayerGap → warning (bars touch); no geometric fix | — | — | KataBottomMainBarRuns.cs:63-65 | KataDy14DrawingTests.A_small_step_that_must_be_cut_is_reported_when_the_two_bars_would_touch:119 | open (DY14 review H1) |
| Cut-off cantilever keeps no bottom bar | — | — | KataBottomMainBarRuns.cs:89 | — | — |
| Hoops, side bars, bottom extras follow span depth | — | — | KataStirrupZoneLayout.cs:44; KataSideBarLayout.cs:62; KataSpanBottomBarLayout.cs:43 | KataDy7DrawingTests:145 | DY7 |
| Row 21 bars ("100;5f25"), row 19 top drop + bars → skipped | — | 19, 21 | KataScopeFilter.cs:46-50 | KataScopeFilterTests:60 | not implemented |
| DY14 quirk: at a CUT support Kata shortens left ends 75 mm (E13/E14, D17/D18, side bars) — not reproduced | — | — | — | KataDy14DrawingTests.At_E_350_the_bars_match_the_drawing_except_the_left_ends_Kata_draws_75_shorter:175 | ⚠️ unexplained |
| Export: span row 21 = −(h − h_first) + zOffset; span row 19 = zOffset | — | — | X/KataRowBuilder.cs:44-45 | TX/KataRowBuilderTests.Row21CarriesGridOffsetAtSupportsAndSoffitStepOnSpans:140 | — (a top-dropped beam reads back as a depth change; geometry check catches > 2 mm) |

## 14. Crossing beams as supports, multi-span, Revit geometry

| Rule | Code | Tests | Verified |
|---|---|---|---|
| Support columns odd/even by row 10 "Cột"/"Nhịp" text; list ends at first empty row-11 cell (C..BZ) | KataDamSheetParser.cs:114-150 | KataDamSheetParserTests.Parse_GoldenKataSample_*:23 | MVP |
| Row 11 support "bxh" / "b*h" / "b/h" → width b, BeamDepth h; plain number → width | KataBarNotationParser.cs:250-271; KataDamSheetParser.cs:185-218 | KataDamSheetParserTests.Parse_CantileverAndBeamSupport_IdentifiesCorrectly:175 | DY7 (I 200x350), DY14 (K 100x350) |
| Support rows 19 (upper col), 20 (crossing width), 21 (crossing offset), 22/23 (grid) parsed, NOT used by layout | KataDamSheetParser.cs:205-209 | — | not implemented |
| Export: merge overlapping supports, dominant column > foundation > beam; beam merged into column = crossing station (row 21 offset, else grid offset); beam-only support writes "bxh"; lean concrete ≤ 100 mm ignored; footing > 6000 along run = strip/raft, not support; joints within 50 mm of a face snap in; free end → zero-width column | X/KataSegmenter.cs:54-92, 130-135; X/KataRowBuilder.cs:28, 52, 69-96; X/KataSupportRules.cs:14-27; KataExport/KataTolerance.cs | TX/KataSegmenterTests.OverlappingSupportsMergeAndColumnWinsOverGirder:54; TX/KataRowBuilderTests.GirderSupportWritesItsOwnSectionInRow11:86, Row21AtAColumnGivesTheCrossingBeamOffsetAndFallsBackToTheGrid:56; TX/KataSupportRulesTests:15/25; TX/KataTieBeamTests | DY7 export |
| Planner: Revit widths, span lengths, per-span depths, b replace sheet values; B5 = first span depth in sheet order | C/Calculators/KataRebarPlanner.cs:81-110 | T/KataRebarPlannerTests.Revit_geometry_replaces_the_sheet_numbers:101; KataDy7DrawingTests.A_beam_support_and_measured_span_depths_plan_without_blocking:159 | DY7 |
| Multi-span: any span count; stations x = 0 at outer face of first support | C/Calculators/KataBeamStations.cs:35-62 | KataMultiSpanPlanTests.A_two_span_run_is_planned_whether_Revit_draws_it_as_one_or_two_beams:14; KataStressAdversarialTests.Calculate_TwelveSpansContinuousBeam_*:237 | DY7 (3 spans), DY14 (4 spans) |

## 15. Stock length 11.7 m / splices

| Item | Status | Evidence |
|---|---|---|
| 11.7 m split, laps, couplers, MaxBarLength setting | **Dropped** (user 2026-10-01): bars whole, laps counted in Revit schedule; old `MaxBarLength`/`SideBarTieSpacing` JSON keys ignored | plans/261001-0900-kata-beam-rules-phase1/plan.md; T/KataSettingsJsonTests.A_file_written_before_the_stock_length_was_dropped_still_reads:30 |
| Cranks ("cổ chai") only at soffit steps (§13); no lap cranks | implemented as §13 | — |

## 16. Hooks of stirrups / ties, bend radius

| Rule | Value | Code | Tests | Verified |
|---|---|---|---|---|
| Inner □/U hooks: ClosedStirrupHookAngle/Factor; C ties (inner C, side-bar, layer): CrossTieHookAngle/Factor; angle ∈ {90,135,180} else default | 135° / 7.5, 180° / 7.5 | KataSettings.cs:17-24; C/Parsers/KataSettingsJson.cs:76-104 | T/KataSettingsTests.Defaults_are_those_of_the_Kata_detail_dialog:11 | — (Kata dialog values, not DWG-measured) |
| OUTER hoop hook = project RebarShape "41"/"T1" hooks, NOT the settings | — | R/KataRebarShapeResolver.cs:9-13; R/KataStirrupSetCreator.cs:37 | — | SEC: Kata hoop 135° with 5·ds tails |
| Revit hook type: exact angle, StirrupTie style first, closest StraightLineMultiplier to factor; none → drawn without hook + warning | — | R/KataRebarHookResolver.cs:19-36; R/KataBarSetCreator.cs:46-72 | — | — |
| Model longitudinal bars: sharp polylines, DimR = 2d metadata only; Revit applies bar type bend diameter | 2d | KataMainBarLayout.cs:200 | — | — |
| Tie wrap: straight part offset by bend radius of bar type ((StirrupTieBendDiameter or StandardHookBendDiameter) + Øbar)/2; schedule DimB/DimC of C tie = 10·ds | — | R/KataBarSetCreator.cs:109-113; KataTieWrap.cs:17-36; C/Calculators/KataStirrupCurveFactory.cs:68-69 | KataTieWrapTests | TIES (Revit template radius 14 for Ø8 vs DWG 16) |
| Section drawing: hoop corner radius (d+ds)/2, tails 5·ds | — | KataSectionBars.cs:54-59 | KataSectionDrawingTests | SEC |

## 17. Geometry check sheet vs Revit ("G1" of the MVP table, not cell G1)

| Rule | Value | Code | Tests | Verified |
|---|---|---|---|---|
| b (B6), h (B5 vs first span in sheet order), every row-11 length, every span depth (B5 − row 21): ≤ 2 mm silent, ≤ 50 warn naming cell, > 50 block; Revit wins | 2 / 50 | C/Calculators/KataSheetGeometryCheck.cs:26-27, 33-80, 111-134 | T/KataSheetGeometryCheckTests (all 9); KataDy7DrawingTests.A_measured_span_depth_off_the_sheet_by_more_than_50_blocks:180 | MVP, live |
| Column count ≠ → block; support/span order must match either direction; symmetric run → preferred direction (Kata Export) else forward | — | KataSheetGeometryCheck.cs:45-66 | A_symmetric_run_follows_the_direction_the_sheet_was_written_in:15, A_different_number_of_columns_blocks:93 | live (DY14 block "7 cột … 9") |
| Joint segment (no support) in run → block | — | KataRebarPlanner.cs:62-78 | — | — |

## 18. Scope filter, blocking, skipped (known not implemented)

| Kind | Item | Code | Test |
|---|---|---|---|
| Block | no span | KataScopeFilter.cs:32-33 | — |
| Block | supports ≠ spans + 1 or any support width ≤ 0 (cantilever, joint) | KataScopeFilter.cs:35-36 | KataScopeFilterTests.A_cantilever_end_blocks:95 |
| Block | B11, B12 empty and G6 ≤ 0 | KataScopeFilter.cs:38-39 | — |
| Block (rules) | J9 a leaves no room for stirrup; row 21 depth ≤ 0; two main layers deeper than shallowest span | KataDetailingRuleBuilder.cs:44-45, 57-58, 61-63 | KataDetailingRuleBuilderTests:54/90 |
| Block (layout) | bars overlapping in a layer; top extra level vs bottom mains; bottom extra vs top bars | KataLayerPositions.cs:53-57; KataSupportTopBarLayout.cs:42-47; KataSpanBottomBarLayout.cs:155-163 | Bars_on_top_of_each_other_block:136, More_layers_than_the_depth_holds_block:258 |
| Skipped | B11/B12 2nd+ groups | KataScopeFilter.cs:41-42 | KataScopeFilterTests:41 |
| Skipped | span row 19 top drop / bars | KataScopeFilter.cs:46-47 | KataScopeFilterTests:60 |
| Skipped | span row 21 bar change with soffit step | KataScopeFilter.cs:49-50 | same |
| Skipped | span row 23 note, support row 24 note | KataScopeFilter.cs:53-54 | KataStirrupSectionParserTests:61 |
| Warned, not drawn | "-" in rows 14-16; unreadable bar tokens; cut not leaving support | KataSupportTopBarLayout.cs:58-61, 225-229 | KataSteppedBeamEdgeTests:86 |
| Silently unused | I1; support rows 19-23 (upper column, crossing beam w/offset, grid); G9 (only cantilever path); B4, B7-B10 | KataDamSheetParser.cs:28-36, 64, 205-209 | — |
| Unreachable | all cantilever layout branches (blocked above) | KataMainBarLayout.cs:88-96; KataStirrupZoneLayout.cs:211-224 | calculator-direct tests only |

## 19. Discrepancies / open items for the rewrite (no fix proposed here)

1. Layer clear gap: model max(30, d) vs Kata section 25 (§11).
2. Bar centre: model raises J9 a to b + ds + d/2 (42); Kata draws c + (d+ds)/2 (38) and DY7 elevation ends at 30 (§1, §3).
3. Side-bar tie straight part: model below bars, Kata section above (§10).
4. End legs: Kata draws legs past beam face (300/400); model clamps to depth + warns (§3).
5. Bottom leg inset at end supports (133 vs Kata 35) is HPRebar-only (§3).
6. DY14 75 mm left-end shortening at a cut support unexplained (§13).
7. Small cut steps (Ø < 16) leave touching bars, warning only (§13); "-" chain from both sides doubles bars, warning only (§5).
8. RoundNearest midpoint (AwayFromZero) unpinned (§6); ZoneRound 50, FirstStirrupOffset 50, MinimumLegGap 25, BarClearSpacing 25, CrankSlope 6 are code constants, not settings.
9. Empty G6 → Ø10 hoops (parser default), not "no stirrups" (§1).
10. docs/specs/kata-beam-rebar-rules.md §1 rows R3/R4/R5, H3-for-rows-14-16, "móc C a400" stale; KataSettings.cs:26 comment says reaches only round up (L/6 cap rounds nearest).
11. Inner stirrups (rows 25-44), uniform J7 tie distribution start/end, I8 = 1 mode, left;right cells: no Kata DWG evidence.

**Status:** DONE
**Summary:** Inventory of ~120 implemented rules across 19 sections with cell/setting, file:line, pinning tests and DWG/live evidence. Main open points: layer gap 30 vs Kata 25, bar centre 42 vs 38, side-tie orientation, unexplained DY14 −75 mm, scope-blocked cantilevers.
