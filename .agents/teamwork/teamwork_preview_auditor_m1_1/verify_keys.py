import os

theme_path = r'HPKata/HPKata.UI/Themes/Theme.xaml'
bridge_path = r'HPKata/HPKata.UI/Themes/MaterialBridge.xaml'

with open(theme_path, 'r', encoding='utf-8') as fp:
    theme_content = fp.read()

with open(bridge_path, 'r', encoding='utf-8') as fp:
    bridge_content = fp.read()

required_keys = [
    'StandardDataGrid',
    'StandardTextBox',
    'StandardComboBox',
    'PrimaryButton',
    'Button.Primary',
    'SecondaryButton',
    'Button.Secondary',
    'Card.Panel',
    'GroupBox.Header',
    'Text.Title',
    'Text.Section',
    'Text.Muted',
    'Text.Bold',
    'Text.Label',
    'Brush.Card'
]

print("=== STYLE KEY AUDIT IN Theme.xaml ===")
all_keys_pass = True
for k in required_keys:
    pattern = f'x:Key="{k}"'
    found = pattern in theme_content
    status = "FOUND (PASS)" if found else "MISSING (FAIL)"
    if not found:
        all_keys_pass = False
    print(f"Key {k:20}: {status}")

print("\n=== StandardFont IN MaterialBridge.xaml ===")
font_found = 'x:Key="StandardFont"' in bridge_content
print(f"StandardFont        : {'FOUND (PASS)' if font_found else 'MISSING (FAIL)'}")

print("\n=== IMPLICIT STYLES IN Theme.xaml ===")
implicit_types = ['TextBlock', 'TextBox', 'ComboBox', 'GroupBox', 'DataGrid', 'DataGridColumnHeader']
all_implicit_pass = True
for t in implicit_types:
    pattern = f'<Style TargetType="{t}"'
    found = pattern in theme_content
    status = "FOUND (PASS)" if found else "MISSING (FAIL)"
    if not found:
        all_implicit_pass = False
    print(f"Implicit Style {t:15}: {status}")

print("\n=== SUMMARY ===")
print(f"All Required Keys Present: {all_keys_pass and font_found}")
print(f"All Implicit Styles Present: {all_implicit_pass}")
