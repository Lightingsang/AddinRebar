import ctypes
import ctypes.wintypes

user32 = ctypes.windll.user32

REVIT_PID = 18364

found_windows = []

def enum_windows_proc(hwnd, lParam):
    pid = ctypes.wintypes.DWORD()
    user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
    if pid.value == REVIT_PID:
        length = user32.GetWindowTextLengthW(hwnd)
        buf = ctypes.create_unicode_buffer(length + 1)
        user32.GetWindowTextW(hwnd, buf, length + 1)
        
        class_buf = ctypes.create_unicode_buffer(256)
        user32.GetClassNameW(hwnd, class_buf, 256)
        
        visible = user32.IsWindowVisible(hwnd)
        found_windows.append({
            "hwnd": hwnd,
            "class": class_buf.value,
            "title": buf.value,
            "visible": bool(visible)
        })
    return True

import subprocess, json

# Use psutil or win32 to get thread IDs
import ctypes.wintypes
ps_cmd = 'Get-Process -Id 18364 | Select-Object -ExpandProperty Threads | Select-Object -ExpandProperty Id'
out = subprocess.check_output(['powershell', '-NoProfile', '-Command', ps_cmd], text=True)
thread_ids = [int(x.strip()) for x in out.strip().splitlines() if x.strip().isdigit()]

WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.wintypes.HWND, ctypes.wintypes.LPARAM)

print(f"Checking {len(thread_ids)} threads...")
for tid in thread_ids:
    def thread_proc(hwnd, lParam):
        length = user32.GetWindowTextLengthW(hwnd)
        buf = ctypes.create_unicode_buffer(length + 1)
        user32.GetWindowTextW(hwnd, buf, length + 1)
        class_buf = ctypes.create_unicode_buffer(256)
        user32.GetClassNameW(hwnd, class_buf, 256)
        visible = user32.IsWindowVisible(hwnd)
        print(f"Thread {tid} -> HWND: {hwnd} | Visible: {bool(visible)} | Class: {class_buf.value} | Title: '{buf.value}'")
        return True
    
    user32.EnumThreadWindows(tid, WNDENUMPROC(thread_proc), 0)
