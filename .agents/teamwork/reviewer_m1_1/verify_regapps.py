regapps = [
    'RebarXdataDCE', 'DceRebarXDataTKT', 'Links_XDataObject', 'Dce_ThepKep',
    'XDATA_APP_SODOCAT', 'XDATA_APP_KHOTHEPDU', 'XDATA_MAT_BANG_MONG', 'XDATA_MONG_COC',
    'SYS_XData_ThangMoi', 'SYS_XData_App', 'NHAN_XDATA_COC', 'NHAN_XDATA_DAM_CONG', 'NHAN_XDATA_MB'
]

with open(r'C:\Dce Pro\DCE Pro.dll', 'rb') as f:
    data = f.read()

print("Verifying 13 CAD RegApps in DCE Pro.dll:")
for app in regapps:
    found_ascii = app.encode('ascii') in data
    found_utf16 = app.encode('utf-16le') in data
    status = "FOUND" if (found_ascii or found_utf16) else "NOT FOUND"
    print(f"  {app:<25}: {status}")
