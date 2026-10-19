import re
import glob

files = [
    "DCEPro_LicenseMoi.cs",
    "DCEPro_LicenseCheckAPI.cs",
    "DCEPro_DceSerinumber.cs",
    "DCEPro_DbimTaiKhoanApi.cs",
    "DCEPro_DceFingerprint.cs",
    "Excel_LicenseMoi.cs",
    "Excel_LicenseCheckAPI.cs",
    "Excel_DceSerinumber.cs",
    "Excel_DbimTaiKhoanApi.cs",
    "Steel_Chung_License.cs"
]

base = r"g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_survey_1\\"

print("=== Analyzing Licensing & DRM Classes ===")
for fname in files:
    path = base + fname
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()
    
    print(f"\n--- {fname} ({len(content.splitlines())} lines) ---")
    
    # URLs / endpoints
    urls = re.findall(r'https?://[^\s"\'<>]+', content)
    if urls:
        print(f"  URLs/Endpoints ({len(urls)}):")
        for u in set(urls):
            print(f"    - {u}")
            
    # Registry keys
    reg = re.findall(r'(?:SOFTWARE|HKEY_[A-Z_]+)\\[^\s"\'<>]+', content, re.IGNORECASE)
    if reg:
        print(f"  Registry paths ({len(reg)}):")
        for r_path in set(reg):
            print(f"    - {r_path}")
            
    # WMI / Hardware queries
    wmi = re.findall(r'(?:Win32_[A-Za-z0-9_]+|ProcessorId|SerialNumber|VolumeSerialNumber|UUID|MACAddress)', content, re.IGNORECASE)
    if wmi:
        print(f"  WMI / Hardware queries ({len(wmi)}):")
        for w in set(wmi):
            print(f"    - {w}")
            
    # Key methods
    methods = re.findall(r'public (?:static )?(?:async Task(?:<[^>]+>)?|[^\(\)\s]+)\s+([A-Za-z0-9_]+)\s*\([^\)]*\)', content)
    clean_m = [m for m in methods if not m.startswith('get_') and not m.startswith('set_')]
    if clean_m:
        print(f"  Public Methods ({len(clean_m)}):")
        for m in clean_m[:15]:
            print(f"    - {m}")
