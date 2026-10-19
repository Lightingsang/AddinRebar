import os, time, sys
from PIL import ImageGrab
import win32gui, win32con

def capture():
    # Find Revit window
    hwnd = win32gui.FindWindow("WWHG", None)
    if not hwnd:
        # Search by title
        def enum_cb(h, acc):
            if "Autodesk Revit" in win32gui.GetWindowText(h):
                acc.append(h)
        acc = []
        win32gui.EnumWindows(enum_cb, acc)
        if acc:
            hwnd = acc[0]

    if not hwnd:
        print("Revit window not found by class/title, capturing primary monitor.")
        img = ImageGrab.grab()
    else:
        print(f"Found Revit window hwnd: {hwnd}, title: '{win32gui.GetWindowText(hwnd)}'")
        try:
            win32gui.ShowWindow(hwnd, win32con.SW_RESTORE)
            win32gui.SetForegroundWindow(hwnd)
            time.sleep(1.0)
        except Exception as e:
            print("Could not bring to foreground:", e)
        
        rect = win32gui.GetWindowRect(hwnd)
        print(f"Revit rect: {rect}")
        # Ensure valid coords
        if rect[2] > rect[0] and rect[3] > rect[1]:
            img = ImageGrab.grab(bbox=rect)
        else:
            img = ImageGrab.grab()

    target_dir = r"C:\Users\STR-HP03\.gemini\antigravity\brain\4cb249bf-78d3-4e3f-aa18-e0869d1403cc"
    out_path = os.path.join(target_dir, "revit_tang1_beams_verified.png")
    img.save(out_path)
    print(f"Saved screenshot to {out_path} ({img.size})")

if __name__ == "__main__":
    capture()
