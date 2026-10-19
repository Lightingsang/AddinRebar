import json, sys
sys.stdout.reconfigure(encoding="utf-8")


def main():
    with open(r'G:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_3\verified_72_footings.json', 'r', encoding='utf-8') as f:
        footings_data = json.load(f)
    footings = footings_data.get('footings', footings_data)

    f_map = {f['id']: f for f in footings}

    sample_ids = ['ĐC-1-05', 'ĐC-4-04', 'ĐC-1-03', 'ĐC-4-08', 'ĐC-4-09', 'ĐC-4-02', 'ĐC-4-03', 'ĐC-3-10', 'ĐC-3-06', 'ĐC-3-04']
    for fid in sample_ids:
        if fid in f_map:
            f = f_map[fid]
            xs = [v['x'] for v in f['vertices']]
            ys = [v['y'] for v in f['vertices']]
            print(f"Footing {fid}: X=[{min(xs)}, {max(xs)}], Y=[{min(ys)}, {max(ys)}], center=({sum(xs)/len(xs):.1f}, {sum(ys)/len(ys):.1f})")

if __name__ == '__main__':
    main()
