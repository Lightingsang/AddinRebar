import re

with open('Form_In_Tem_Thep.cs', 'r', encoding='utf-8') as f:
    text = f.read()

# Find controls
controls = set(re.findall(r'(?:Button|TextBox|ComboBox|Label|DataGridView|CheckBox|RadioButton|PictureBox)\s+([A-Za-z0-9_]+)', text))
print("--- CONTROLS IN Form_In_Tem_Thep ---")
for c in sorted(controls):
    print("  ", c)

# Find methods
methods = re.findall(r'(?:public|private|protected|internal)\s+(?:static\s+)?(?:void|int|double|string|bool|[A-Za-z0-9_<>]+)\s+([A-Za-z0-9_]+)\s*\(([^)]*)\)', text)
print("\n--- METHODS IN Form_In_Tem_Thep ---")
for m, args in methods:
    if not m.startswith('Dispose') and not m.startswith('InitializeComponent'):
        print(f"   {m}({args})")
