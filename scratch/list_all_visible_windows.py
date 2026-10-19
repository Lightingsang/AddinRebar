import ctypes, ctypes.wintypes

user32 = ctypes.windll.user32

def enum_proc(hwnd, _):
    if user32.IsWindowVisible(hwnd):
        length = user32.GetWindowTextLengthW(hwnd)
        if length > 0:
            buf = ctypes.create_unicode_buffer(length + 1)
            user32.GetWindowTextW(hwnd, buf, length + 1)
            pid = ctypes.wintypes.DWORD()
            user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
            title = buf.value.strip()
            if title and any(k in title.lower() for k in ["revit", "hardware", "acceleration", "autodesk", "security"]):
                print(f"PID: {pid.value} | HWND: {hwnd} | Title: '{title}'")
    return True

WNDENUM = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.wintypes.HWND, ctypes.wintypes.LPARAM)
user32.EnumWindows(WNDENUM(enum_proc), 0)
