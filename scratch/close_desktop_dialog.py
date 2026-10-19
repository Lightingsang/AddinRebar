import ctypes, ctypes.wintypes

user32 = ctypes.windll.user32
kernel32 = ctypes.windll.kernel32

hDesk = user32.GetThreadDesktop(kernel32.GetCurrentThreadId())
print(f"Current Desktop Handle: {hDesk}")

def enum_proc(hwnd, _):
    length = user32.GetWindowTextLengthW(hwnd)
    if length > 0:
        buf = ctypes.create_unicode_buffer(length + 1)
        user32.GetWindowTextW(hwnd, buf, length + 1)
        title = buf.value.strip()
        class_buf = ctypes.create_unicode_buffer(256)
        user32.GetClassNameW(hwnd, class_buf, 256)
        print(f"HWND: {hwnd} | Class: {class_buf.value} | Title: '{title}'")
        if "Hardware Acceleration" in title or "Revit" in title:
            print("--> FOUND TARGET WINDOW! Sending WM_CLOSE...")
            user32.PostMessageW(hwnd, 0x0010, 0, 0) # WM_CLOSE = 0x0010
    return True

DESKTOPENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.wintypes.HWND, ctypes.wintypes.LPARAM)
user32.EnumDesktopWindows(hDesk, DESKTOPENUMPROC(enum_proc), 0)
