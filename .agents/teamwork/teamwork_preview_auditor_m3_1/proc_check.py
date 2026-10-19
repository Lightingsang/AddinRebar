import subprocess
import os

print("=== CHECKING RUNNING PROCESSES ===")
output = subprocess.check_output(['tasklist', '/FI', 'IMAGENAME eq dotnet.exe', '/FO', 'CSV'], text=True)
print(output)

output2 = subprocess.check_output(['wmic', 'process', 'where', "name='dotnet.exe'", 'get', 'processid,commandline'], text=True)
print(output2)
