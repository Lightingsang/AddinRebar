import ctypes, ctypes.wintypes

user32 = ctypes.windll.user32
HWND = 18746436

WM_KEYDOWN = 0x0100
WM_KEYUP = 0x0101
VK_ESCAPE = 0x1B
VK_RETURN = 0x0D
VK_SPACE = 0x20

print("Posting ESC to dialog...")
user32.PostMessageW(HWND, WM_KEYDOWN, VK_ESCAPE, 0)
user32.PostMessageW(HWND, WM_KEYUP, VK_ESCAPE, 0)

print("Posting RETURN to dialog...")
user32.PostMessageW(HWND, WM_KEYDOWN, VK_RETURN, 0)
user32.PostMessageW(HWND, WM_KEYUP, VK_RETURN, 0)

print("Posting SPACE to dialog...")
user32.PostMessageW(HWND, WM_KEYDOWN, VK_SPACE, 0)
user32.PostMessageW(HWND, WM_KEYUP, VK_SPACE, 0)

print("Done.")
