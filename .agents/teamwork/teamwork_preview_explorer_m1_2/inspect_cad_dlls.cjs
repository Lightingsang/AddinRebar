const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

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

function runRea(cmd, dllPath) {
  const fullCmd = `${REA_CLI} ${cmd} "${dllPath}" --format json`;
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

console.log('Testing REA version:');
console.log(execSync(`${REA_CLI} --version`, { encoding: 'utf8' }).trim());
