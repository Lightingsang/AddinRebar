const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const REA_CLI = 'node "C:\\Users\\STR-HP03\\AppData\\Roaming\\npm\\node_modules\\rea-agents\\scripts\\rea.mjs"';

const CAD_DLLS = [
  'Kata_pro64_Cad2025.dll',
  'Kata_pro64_Cad2013.dll',
  'Kata_pro64_GstarCad.dll',
  'Kata_pro64_GstarCad2025.dll',
  'Kata_pro64_ZwCad.dll'
];

function runRea(op, dllPath) {
  const fullCmd = `${REA_CLI} ${op} "${dllPath}" --format json`;
  try {
    const stdout = execSync(fullCmd, { maxBuffer: 150 * 1024 * 1024, encoding: 'utf8' });
    return JSON.parse(stdout);
  } catch (err) {
    if (err.stdout) {
      try {
        return JSON.parse(err.stdout.toString('utf8'));
      } catch (e) {
        return { error: err.message, stdout: err.stdout.toString('utf8') };
      }
    }
    return { error: err.message, stderr: err.stderr ? err.stderr.toString('utf8') : null };
  }
}

const results = {};

for (const dll of CAD_DLLS) {
  const fullPath = `C:\\kata_pro\\${dll}`;
  console.log(`\n=== Processing ${dll} ===`);
  
  console.log(`Running inspect-managed-artifact...`);
  const artifact = runRea('inspect-managed-artifact', fullPath);
  
  console.log(`Running inspect-pe-resources...`);
  const peRes = runRea('inspect-pe-resources', fullPath);
  
  console.log(`Running inspect-managed-native-boundaries...`);
  const natBound = runRea('inspect-managed-native-boundaries', fullPath);
  
  console.log(`Running inspect-managed-members...`);
  const members = runRea('inspect-managed-members', fullPath);
  
  // Extract command method attributes
  const cmdAttrs = [];
  const otherCommandAttrs = [];
  const allAttrs = artifact.normalized_result?.attributes || [];
  for (const attr of allAttrs) {
    const typeName = attr.type_name || '';
    if (typeName.toLowerCase().includes('command') || typeName.toLowerCase().includes('method')) {
      cmdAttrs.push(attr);
    }
  }
  
  results[dll] = {
    artifact_evidence_id: artifact.evidence_id,
    pe_resources_evidence_id: peRes.evidence_id,
    native_boundaries_evidence_id: natBound.evidence_id,
    members_evidence_id: members.evidence_id,
    sha256: artifact.normalized_result?.artifact?.sha256,
    byte_length: artifact.normalized_result?.artifact?.byte_length,
    pe: artifact.normalized_result?.pe,
    classification: artifact.normalized_result?.classification,
    assembly: artifact.normalized_result?.assembly,
    target_frameworks: artifact.normalized_result?.target_frameworks,
    references_count: artifact.normalized_result?.references?.length,
    references: artifact.normalized_result?.references,
    attributes_count: allAttrs.length,
    command_attributes_count: cmdAttrs.length,
    command_attributes: cmdAttrs,
    pe_resources_summary: peRes.normalized_result?.coverage,
    pe_resources_list: peRes.normalized_result?.resources?.map(r => ({
      index: r.index,
      type: r.type,
      name: r.name,
      language: r.language,
      payload_sha256: r.payload?.sha256,
      payload_bytes: r.payload?.location?.bytes,
      payload_offset: r.payload?.location?.offset
    })),
    native_boundaries_summary: natBound.normalized_result?.summary,
    native_boundaries_coverage: natBound.normalized_result?.coverage,
    members_coverage: members.normalized_result?.coverage,
    members_limitations: members.normalized_result?.limitations
  };
}

fs.writeFileSync(
  path.join(__dirname, 'cad_dlls_analysis.json'),
  JSON.stringify(results, null, 2),
  'utf8'
);

console.log('\nWrote cad_dlls_analysis.json successfully!');
