import ctypes, ctypes.wintypes

user32 = ctypes.windll.user32
REVIT_PID = 26264

dialogs = []

def enum_proc(hwnd, lParam):
    pid = ctypes.wintypes.DWORD()
    user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
    if pid.value == REVIT_PID:
        class_buf = ctypes.create_unicode_buffer(256)
        user32.GetClassNameW(hwnd, class_buf, 256)
        
        text_buf = ctypes.create_unicode_buffer(256)
        user32.GetWindowTextW(hwnd, text_buf, 256)
        
        visible = user32.IsWindowVisible(hwnd)
        print(f"Revit HWND: {hwnd}, Class: '{class_buf.value}', Title: '{text_buf.value}', Visible: {bool(visible)}")
        
        # Check child windows/buttons
        def child_proc(child_hwnd, _):
            c_class = ctypes.create_unicode_buffer(256)
            user32.GetClassNameW(child_hwnd, c_class, 256)
            c_text = ctypes.create_unicode_buffer(256)
            user32.GetWindowTextW(child_hwnd, c_text, 256)
            c_id = user32.GetDlgCtrlID(child_hwnd)
            print(f"   Child HWND: {child_hwnd}, ID: {c_id}, Class: '{c_class.value}', Text: '{c_text.value}'")
            # If button matches 'Always Load' or ID 1001
            if c_id == 1001 or "Always Load" in c_text.value or "Luôn tải" in c_text.value:
                print(f"--> CLICKING ALWAYS LOAD (HWND: {child_hwnd})")
                user32.SendMessageW(child_hwnd, 0x00F5, 0, 0) # BM_CLICK = 0x00F5
            return True
            
        WNDENUMCHILD = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.wintypes.HWND, ctypes.wintypes.LPARAM)
        user32.EnumChildWindows(hwnd, WNDENUMCHILD(child_proc), 0)
    return True

WNDENUM = ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.wintypes.HWND, ctypes.wintypes.LPARAM)
user32.EnumWindows(WNDENUM(enum_proc), 0)
