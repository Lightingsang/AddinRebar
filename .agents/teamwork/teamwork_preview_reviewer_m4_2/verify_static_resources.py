import glob
import re
import os

views = glob.glob('HPKata/HPKata.UI/Views/*.xaml')
print(f"Total view files found: {len(views)}")

theme_path = 'HPKata/HPKata.UI/Themes/Theme.xaml'
theme_content = open(theme_path, encoding='utf-8').read()
theme_keys = set(re.findall(r'x:Key="([^"]+)"', theme_content))

mat_path = 'HPKata/HPKata.UI/Themes/MaterialBridge.xaml'
mat_content = open(mat_path, encoding='utf-8').read()
mat_keys = set(re.findall(r'x:Key="([^"]+)"', mat_content))

dark_path = 'HPKata/HPKata.UI/Themes/ThemeDark.xaml'
dark_keys = set(re.findall(r'x:Key="([^"]+)"', open(dark_path, encoding='utf-8').read()))

light_path = 'HPKata/HPKata.UI/Themes/ThemeLight.xaml'
light_keys = set(re.findall(r'x:Key="([^"]+)"', open(light_path, encoding='utf-8').read()))

all_theme_keys = theme_keys | mat_keys | dark_keys | light_keys

print(f"Theme keys count: {len(all_theme_keys)}")

total_static_refs = 0
unresolved_total = 0

for v in sorted(views):
    content = open(v, encoding='utf-8').read()
    local_keys = set(re.findall(r'x:Key="([^"]+)"', content))
    refs = re.findall(r'\{StaticResource\s+([^}]+)\}', content)
    cleaned_refs = [r.split(',')[0].strip() for r in refs]
    total_static_refs += len(cleaned_refs)
    
    unresolved = []
    for r in cleaned_refs:
        if r not in local_keys and r not in all_theme_keys:
            unresolved.append(r)
    
    status = "OK" if not unresolved else f"UNRESOLVED: {unresolved}"
    print(f"{os.path.basename(v)}: {len(cleaned_refs)} refs, {len(local_keys)} local keys -> {status}")
    if unresolved:
        unresolved_total += len(unresolved)

print(f"\nTotal StaticResource references: {total_static_refs}")
print(f"Total Unresolved: {unresolved_total}")
