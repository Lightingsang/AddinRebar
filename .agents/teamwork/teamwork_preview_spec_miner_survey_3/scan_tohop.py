import re
import os

filepath = 'Form_To_Hop_Thep.cs'
with open(filepath, 'r', encoding='utf-8') as f:
    text = f.read()

print('File length:', len(text))

# Search for strings
strings = set(re.findall(r'"([^"\\]{3,80})"', text))
vietnamese_strings = [s for s in strings if any(c in 'àáạảãâầấậẩẫăằắặẳẵèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđĐ' for c in s.lower())]

print('\n--- VIETNAMESE STRINGS IN Form_To_Hop_Thep ---')
for s in sorted(vietnamese_strings):
    print('  ', s)

# Search for button / control names
controls = set(re.findall(r'(?:Button|TextBox|ComboBox|Label|DataGridView|CheckBox|RadioButton)\s+([A-Za-z0-9_]+)', text))
print('\n--- CONTROLS IN Form_To_Hop_Thep ---')
for c in sorted(controls):
    print('  ', c)

# Search for methods
methods = re.findall(r'(?:public|private|protected|internal)\s+(?:static\s+)?(?:void|int|double|string|bool|[A-Za-z0-9_<>]+)\s+([A-Za-z0-9_]+)\s*\(([^)]*)\)', text)
print('\n--- METHODS IN Form_To_Hop_Thep ---')
for m, args in methods[:50]:
    if not m.startswith('Dispose') and not m.startswith('InitializeComponent'):
        print(f'   {m}({args})')
