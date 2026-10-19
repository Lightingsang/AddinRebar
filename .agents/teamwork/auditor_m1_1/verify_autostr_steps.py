import os

classes_file = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\dce_pro_classes.txt'
if os.path.exists(classes_file):
    with open(classes_file, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
    
    steps = [
        'AutoStrStep01_TaoSheetSet', 'AutoStrStep02_LuoiTruc', 'AutoStrStep02b_ExrefCot',
        'AutoStrStep03_DinhViCot', 'AutoStrStep04_MBKC', 'AutoStrStep05_DuongPhanTichLuc',
        'AutoStrStep06_TaiTuong', 'AutoStrStep07_BuildEtabsModel', 'AutoStrStep08_TinhToanDamExcel',
        'AutoStrStep09_VeChiTietDam', 'AutoStrStep10_TinhToanCotExcel', 'AutoStrStep11_VeChiTietCotVach',
        'AutoStrStep12a_TinhToanSan', 'AutoStrStep12_VeChiTietSan', 'AutoStrStep13_MBMongCoc',
        'AutoStrStep14_DPTLMong', 'AutoStrStep15_EtabsMong', 'AutoStrStep16_TTMong',
        'AutoStrStep17_VeMong', 'AutoStrStep18_TTGiangM', 'AutoStrStep19_VeGiangM',
        'AutoStrStep20_CKPhu', 'AutoStrStep21_DanhMuc', 'AutoStrStep22_MoSheetSet',
        'AutoStrStep23_InAn', 'AutoStrStep24_TMTT'
    ]
    
    matched = 0
    for s in steps:
        found = s in content
        if found:
            matched += 1
        print(f"  {s}: {'FOUND' if found else 'NOT FOUND'}")
    print(f"\nMatched {matched} / {len(steps)} AutoStr steps in DCE Pro.dll classes.")
else:
    print('classes_file does not exist')
