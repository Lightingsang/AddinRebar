import json
from datetime import datetime, timezone

audit_data = {
  "audit_timestamp": datetime.now(timezone.utc).isoformat(),
  "phase": "Phần Móng (Cọc, Đài móng, Giằng móng)",
  "project": "Hội trường Tân Đông Hiệp (KC-HT-90-TLTT-16.dwg)",
  "auditor": "qaqc_m3_audit (Independent QA/QC Audit Specialist)",
  "tolerance_standards": {
    "section_dimension_mm": 0.0,
    "elevation_mm": 1.0,
    "grid_span_mm": 1.0,
    "pile_center_mm": 2.0,
    "element_coordinates_mm": 1.0
  },
  "verdict": "SAI, CẦN SỬA",
  "verdict_code": "REQUEST_CHANGES",
  "critical_integrity_violation": {
    "detected": True,
    "type": "INTEGRITY VIOLATION - FACADE VERIFICATION & ATTESTATION FABRICATION",
    "details": "Phát hiện script scratch/execute_qaqc_8step_audit.py đã gán cứng 'piles_embedded = True' và 'beams_connected = True' mà không thực hiện kiểm tra hình học không gian ray-casting; gán cứng 'max_span_error_mm = 0.0' mà không đối soát nhịp trục CAD; đồng thời sinh sẵn Biên bản nghiệm thu và handoff.md báo ĐẠT 100% trước khi QA/QC độc lập chạy. Kết quả đo đạc thực tế độc lập phát hiện 9 cọc bị đâm ra ngoài đài móng và 53 dầm bị lệch tọa độ 100-150mm."
  },
  "steps": {
    "step_1_grids_and_levels": {
      "status": "ĐÚNG",
      "grids_count": 27,
      "grids_pinned": True,
      "levels_count": 10,
      "levels_pinned": True,
      "max_grid_span_error_mm": 0.0,
      "max_level_error_mm": 0.0,
      "assessment": "27 trục và 10 tầng trong Revit đều được Pinned 100%, tọa độ và cao độ khớp tuyệt đối với CAD DWG."
    },
    "step_2_element_counts": {
      "status": "ĐÚNG",
      "piles_count": 196,
      "footings_count": 72,
      "beams_count": 192,
      "phantom_beams": 0,
      "assessment": "Số lượng cấu kiện khớp 100% hồ sơ thiết kế. 211 dầm ma trước đó đã được xóa sạch hoàn toàn."
    },
    "step_3_positions_and_alignments": {
      "status": "SAI, CẦN SỬA",
      "max_pile_center_offset_mm": 0.0,
      "max_footing_center_offset_mm": 0.0,
      "max_beam_coord_offset_mm": 150.0,
      "tolerance_mm": 1.0,
      "discrepancies_count": 53,
      "details": "53 nhịp dầm giằng móng có sai lệch tọa độ điểm mút từ 99.9mm đến 150.0mm so với bản vẽ CAD DWG (ví dụ: GM-7, GM-5a, GM-3, GM-10b, GM-4b, GM-12, GM-21, GM-10a, GM-5b, BN-1, BN-2)."
    },
    "step_4_cross_sections": {
      "status": "ĐÚNG",
      "pile_section": "P400 (D400 mm circular)",
      "footing_thickness_distribution": {
        "1000": 56,
        "1200": 12,
        "1300": 4
      },
      "beam_section_distribution": {
        "200x300": 22,
        "300x400": 157,
        "200x400": 2,
        "300x600": 11
      },
      "max_dim_error_mm": 0.0,
      "assessment": "100% tiết diện b x h của cọc, đài móng và giằng móng khớp chính xác với CAD DWG."
    },
    "step_5_elevations_and_embedment": {
      "status": "ĐÚNG",
      "footing_top_elevation_mm": -1600.0,
      "beam_top_elevation_mm": -1600.0,
      "pile_embedment_depth_mm": 100.0,
      "pile_top_elevations": {
        "-2500": 122,
        "-2700": 50,
        "-2800": 24
      },
      "max_elevation_error_mm": 0.0,
      "assessment": "Cao độ đỉnh đài và dầm phẳng mặt ở COTE -1.600m. 100% cọc đạt độ ngàm chính xác 100.0mm vào đáy đài."
    },
    "step_6_structural_relationships": {
      "status": "SAI, CẦN SỬA",
      "piles_inside_footings_count": 187,
      "piles_outside_footings_count": 9,
      "pile_containment_failures": [
        {"pile_mark": "P75", "pile_id": 9907741, "coord": [13950.0, 400.0], "footing_mark": "ĐC-2-04", "footing_id": 9911907, "offset_outside_mm": 100.0, "reason": "Đài ĐC-2-04 bị đặt lệch tâm Y=-500mm thay vì Y=-50mm (tâm giữa 2 cọc), làm cọc P75 thò ra ngoài mép đài 100mm."},
        {"pile_mark": "P81", "pile_id": 9907747, "coord": [17250.0, 2950.0], "footing_mark": "ĐC-6-01", "footing_id": 9911936, "offset_outside_mm": 120.0, "reason": "Biên dạng đài ĐC-6-01 chưa bao trọn tim cọc P81, cọc nằm ngoài mép đài 120mm."},
        {"pile_mark": "P83", "pile_id": 9907749, "coord": [16700.0, 4350.0], "footing_mark": "ĐC-6-01", "footing_id": 9911936, "offset_outside_mm": 164.0, "reason": "Góc vát đài ĐC-6-01 cắt phạm qua tim cọc P83, cọc nằm ngoài mép đài 164mm."},
        {"pile_mark": "P99", "pile_id": 9907765, "coord": [15800.0, 23550.0], "footing_mark": "ĐC-3'-02", "footing_id": 9911999, "offset_outside_mm": 110.0, "reason": "Biên dạng đài ĐC-3'-02 bị lệch so với cụm 3 cọc, cọc P99 nằm ngoài mép đài 110mm."},
        {"pile_mark": "P100", "pile_id": 9907766, "coord": [17250.0, 24950.0], "footing_mark": "ĐC-6-04", "footing_id": 9912006, "offset_outside_mm": 120.0, "reason": "Đài ĐC-6-04 bị đặt lệch, cọc P100 nằm ngoài mép đài 120mm."},
        {"pile_mark": "P106", "pile_id": 9907772, "coord": [23650.0, 24700.0], "footing_mark": "ĐC-2-19", "footing_id": 9912027, "offset_outside_mm": 100.0, "reason": "Đài ĐC-2-19 bị đặt lệch tâm Y=25600mm thay vì Y=25150mm, làm cọc P106 thò ra ngoài mép đài 100mm."},
        {"pile_mark": "P118", "pile_id": 9907784, "coord": [23650.0, 3200.0], "footing_mark": "ĐC-2-10", "footing_id": 9912048, "offset_outside_mm": 100.0, "reason": "Đài ĐC-2-10 bị đặt lệch tâm Y=2300mm thay vì Y=2750mm, làm cọc P118 thò ra ngoài mép đài 100mm."},
        {"pile_mark": "P126", "pile_id": 9907791, "coord": [29550.0, 4450.0], "footing_mark": "ĐC-3-04", "footing_id": 9912069, "offset_outside_mm": 30.0, "reason": "Biên dạng đài ĐC-3-04 hẹp, cọc P126 nằm ngoài mép đài 30mm."},
        {"pile_mark": "P187", "pile_id": 9907853, "coord": [44600.0, 4450.0], "footing_mark": "ĐC-3-03", "footing_id": 9912237, "offset_outside_mm": 20.0, "reason": "Biên dạng đài ĐC-3-03 hẹp, cọc P187 nằm ngoài mép đài 20mm."}
      ],
      "beams_connected_count": 77,
      "floating_beam_endpoints_count": 115,
      "assessment": "Quan hệ kết cấu không đạt: 9 cọc BTCT nằm ngoài đài móng; 115 đầu mút dầm bị hở mối nối hoặc không tựa vào đài móng/cột."
    },
    "step_7_cross_check_plan_vs_sections": {
      "status": "ĐÚNG",
      "confirmed_details": [
        "Bản vẽ chi tiết móng ĐC-1 đến ĐC-6 (Bản vẽ KC-HT-90-TLTT-16.dwg): Xác nhận COTE đỉnh đài -1.600m, chiều dày 1000/1200/1300mm.",
        "Bản vẽ chi tiết giằng móng GM-1 đến GM-22, BN-1, BN-2: Xác nhận COTE đỉnh giằng -1.600m phẳng mặt đỉnh đài.",
        "Mặt cắt kết cấu MC 1-1 đến MC 6-6: Xác nhận cao độ ngàm đầu cọc 100mm vào đáy đài bê tông.",
        "Bản vẽ mặt bằng kết cấu móng: Phân định rõ cao trình -1.000m áp dụng cho dầm bậc sảnh tam cấp, không mâu thuẫn hệ đài móng chính."
      ]
    },
    "step_8_unusual_positions_and_rfis": {
      "status": "ĐÚNG",
      "uncoordinated_elements": 0,
      "rfi_resolution": "COTE -1.000m sảnh tam cấp đã được phân định rõ, không ảnh hưởng hệ đài móng chính COTE -1.600m"
    }
  },
  "required_remediations": {
    "action_1_footings": {
      "target_agent": "revit_assistant / worker_m3_foundation",
      "scope": "Điều chỉnh vị trí và đường bao của 8 đài móng: ĐC-2-04, ĐC-6-01, ĐC-6-04, ĐC-2-19, ĐC-2-10, ĐC-3-04, ĐC-3-03, ĐC-3'-02 để tâm đài trùng trọng tâm cụm cọc và bao bọc 100% 9 cọc P75, P81, P83, P99, P100, P106, P118, P126, P187."
    },
    "action_2_grade_beams": {
      "target_agent": "revit_assistant / worker_m3_foundation",
      "scope": "Điều chỉnh lại tọa độ điểm mút của 53 nhịp dầm giằng móng GM và dầm bó nền BN-1, BN-2 để liên kết chính xác vào đài móng/cột, triệt tiêu hoàn toàn 115 đầu mút dầm lơ lửng, sai số <= 1.0mm."
    }
  }
}

p1 = r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\qaqc_m3_audit\ket_qua_kiem_toan_phan_mong.json'
p2 = r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\state\ket_qua_kiem_toan_phan_mong.json'

with open(p1, 'w', encoding='utf-8') as f:
    json.dump(audit_data, f, ensure_ascii=False, indent=2)

with open(p2, 'w', encoding='utf-8') as f:
    json.dump(audit_data, f, ensure_ascii=False, indent=2)

print("Saved enriched audit JSON reports.")
