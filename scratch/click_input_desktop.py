import ctypes, ctypes.wintypes

user32 = ctypes.windll.user32
kernel32 = ctypes.windll.kernel32

GENERIC_ALL = 0x10000000
hInputDesk = user32.OpenInputDesktop(0, False, GENERIC_ALL)
print(f"OpenInputDesktop: {hInputDesk}")

if not hInputDesk:
    # Try with DESKTOP_ENUMERATE (0x0040) | DESKTOP_WRITEOBJECTS (0x0080) | DESKTOP_READOBJECTS (0x0001)
    hInputDesk = user32.OpenInputDesktop(0, False, 0x0141)
    print(f"OpenInputDesktop (fallback): {hInputDesk}")

found = []
def enum_proc(hwnd, _):
    class_buf = ctypes.create_unicode_buffer(256)
    user32.GetClassNameW(hwnd, class_buf, 256)
    length = user32.GetWindowTextLengthW(hwnd)
    text_buf = ctypes.create_unicode_buffer(length + 1) if length > 0 else ctypes.create_unicode_buffer(1)
    if length > 0:
        user32.GetWindowTextW(hwnd, text_buf, length + 1)
    
    vis = user32.IsWindowVisible(hwnd)
    pid = ctypes.wintypes.DWORD()
    user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
    
    t = text_buf.value.strip()
    c = class_buf.value.strip()
    
    if vis and (c == "#32770" or "Revit" in t or "Hardware" in t or "Autodesk" in t):
        print(f"PID: {pid.value} | HWND: {hwnd} | Class: {c} | Title: '{t}'")
        found.append(hwnd)
    return True

DESKTOPENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.wintypes.HWND, ctypes.wintypes.LPARAM)

if hInputDesk:
    user32.EnumDesktopWindows(hInputDesk, DESKTOPENUMPROC(enum_proc), 0)
else:
    print("Could not open input desktop. Error:", kernel32.GetLastError())
