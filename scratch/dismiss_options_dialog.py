import ctypes, ctypes.wintypes

user32 = ctypes.windll.user32
HWND = 18746436

WM_CLOSE = 0x0010
WM_COMMAND = 0x0111
IDCANCEL = 2
IDOK = 1
BM_CLICK = 0x00F5

print("Sending WM_CLOSE to HWND:", HWND)
user32.PostMessageW(HWND, WM_CLOSE, 0, 0)
user32.PostMessageW(HWND, WM_COMMAND, IDCANCEL, 0)
user32.PostMessageW(HWND, WM_COMMAND, IDOK, 0)

# Also check child buttons
def child_proc(child_hwnd, _):
    cid = user32.GetDlgCtrlID(child_hwnd)
    text_buf = ctypes.create_unicode_buffer(256)
    user32.GetWindowTextW(child_hwnd, text_buf, 256)
    print(f"Child HWND: {child_hwnd}, ID: {cid}, Text: '{text_buf.value}'")
    if "Close" in text_buf.value or "Đóng" in text_buf.value or cid in [1, 2, 1001, 1002]:
        print(f"--> Clicking child button {child_hwnd}")
        user32.SendMessageW(child_hwnd, BM_CLICK, 0, 0)
    return True

WNDENUMCHILD = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.wintypes.HWND, ctypes.wintypes.LPARAM)
user32.EnumChildWindows(HWND, WNDENUMCHILD(child_proc), 0)
print("Done.")
