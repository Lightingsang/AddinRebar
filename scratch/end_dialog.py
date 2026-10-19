import ctypes

user32 = ctypes.windll.user32
HWND = 18746436

print("Calling EndDialog...")
res = user32.EndDialog(HWND, 8)
print("EndDialog result (8):", res)

res2 = user32.EndDialog(HWND, 2)
print("EndDialog result (2):", res2)

res3 = user32.DestroyWindow(HWND)
print("DestroyWindow result:", res3)
