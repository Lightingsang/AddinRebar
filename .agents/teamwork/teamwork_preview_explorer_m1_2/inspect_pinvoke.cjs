const { execSync } = require('child_process');
const stdout = execSync('node "C:\\Users\\STR-HP03\\AppData\\Roaming\\npm\\node_modules\\rea-agents\\scripts\\rea.mjs" inspect-managed-native-boundaries "C:\\kata_pro\\Kata_pro64_Cad2025.dll" --format json', { maxBuffer: 50 * 1024 * 1024, encoding: 'utf8' });
const data = JSON.parse(stdout);
for (let i = 0; i < 5; i++) {
  const p = data.normalized_result.pinvoke_imports[i];
  console.log(`[${i}] Token: ${p.token}, Import: ${p.import_name}, Scope: ${p.import_scope_name}, Member: ${p.member_name}`);
}
