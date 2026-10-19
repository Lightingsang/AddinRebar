import subprocess
import os
import sys

sys.stdout.reconfigure(encoding='utf-8')

cli_exe = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPBim\src\HPBim.Cli\bin\Release\net10.0\HPBim.Cli.exe'

valid_json = """{
  "Version": "1.0",
  "Levels": [
    { "Name": "TANG 1", "ElevationM": 0.0, "Story": true },
    { "Name": "TANG 2", "ElevationM": 3.6, "Story": true }
  ],
  "Grids": [
    { "Name": "X1", "StartPoint": {"X": 0, "Y": 0, "Z": 0}, "EndPoint": {"X": 0, "Y": 5000, "Z": 0} }
  ],
  "Columns": [
    { "Id": "C1", "BaseLevel": "TANG 1", "TopLevel": "TANG 2", "WidthMm": 220, "DepthMm": 220, "Location": {"X": 0, "Y": 0, "Z": 0} }
  ],
  "Beams": [
    { "Id": "B1", "Level": "TANG 2", "WidthMm": 200, "HeightMm": 350, "LengthMm": 5000, "StartPoint": {"X": 0, "Y": 0, "Z": 3600}, "EndPoint": {"X": 0, "Y": 5000, "Z": 3600} }
  ],
  "Slabs": [
    { "Id": "S1", "Level": "TANG 2", "ThicknessMm": 100, "Boundary": [{"X": 0, "Y": 0}, {"X": 5000, "Y": 0}, {"X": 5000, "Y": 5000}, {"X": 0, "Y": 5000}] }
  ],
  "Walls": [
    { "Id": "W1", "BaseLevel": "TANG 1", "TopLevel": "TANG 2", "ThicknessMm": 220, "LengthMm": 5000, "StartPoint": {"X": 0, "Y": 0}, "EndPoint": {"X": 0, "Y": 5000} }
  ],
  "Rooms": [],
  "Rebars": [
    { "Id": "R1", "HostElementId": "C1", "BarMark": 1, "DiameterMm": 18, "TotalWeightKg": 50.5, "Zone": "Columns", "CoverMm": 25.0 }
  ],
  "Fixtures": [],
  "Pipes": []
}"""

temp_file = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_gen2\temp_audit_model.json'
with open(temp_file, 'w', encoding='utf-8') as f:
    f.write(valid_json)

try:
    print('=== RUNNING VALIDATE ===')
    res_val = subprocess.run([cli_exe, 'validate', '--input', temp_file], capture_output=True, text=True, encoding='utf-8')
    print(res_val.stdout)
    if res_val.stderr:
        print('STDERR:', res_val.stderr)
    print('Validate Exit Code:', res_val.returncode)

    print('=== RUNNING ESTIMATE ===')
    boq_out = r'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\auditor_m4_gen2\temp_boq.txt'
    res_est = subprocess.run([cli_exe, 'estimate', '--input', temp_file, '--output', boq_out], capture_output=True, text=True, encoding='utf-8')
    print(res_est.stdout)
    if res_est.stderr:
        print('STDERR:', res_est.stderr)
    print('Estimate Exit Code:', res_est.returncode)

    if os.path.exists(boq_out):
        print('=== EXPORTED BOQ FILE CONTENT ===')
        with open(boq_out, 'r', encoding='utf-8') as f:
            print(f.read())
        os.remove(boq_out)
finally:
    if os.path.exists(temp_file):
        os.remove(temp_file)
