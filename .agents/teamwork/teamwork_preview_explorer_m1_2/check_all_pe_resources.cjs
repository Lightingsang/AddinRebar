const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

const REA_CLI = 'node "C:\\Users\\STR-HP03\\AppData\\Roaming\\npm\\node_modules\\rea-agents\\scripts\\rea.mjs"';

const TARGET_DLLS = [
  'Kata_pro64_Cad2025.dll',
  'Kata_pro64_Cad2013.dll',
  'Kata_pro64_GstarCad.dll',
  'Kata_pro64_GstarCad2025.dll',
  'Kata_pro64_ZwCad.dll',
  'Kata_pro64.dll',
  'kata_pro.dll'
];

for (const dll of TARGET_DLLS) {
  const p = `C:\\kata_pro\\${dll}`;
  const out = execSync(`${REA_CLI} inspect-pe-resources "${p}" --format json`, { encoding: 'utf8' });
  const data = JSON.parse(out);
  console.log(`=== ${dll} ===`);
  console.log(`evidence_id: ${data.evidence_id}`);
  console.log(`format: ${data.normalized_result.format}, machine: ${data.normalized_result.machine}`);
  console.log(`resources count: ${data.normalized_result.resources.length}`);
  for (const r of data.normalized_result.resources) {
    console.log(`  - [${r.index}] type: ${JSON.stringify(r.type)}, name: ${JSON.stringify(r.name)}, lang: ${JSON.stringify(r.language)}, bytes: ${r.payload?.location?.bytes}, sha256: ${r.payload?.sha256}`);
  }
}
