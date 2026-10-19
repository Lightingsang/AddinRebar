const fs = require('fs');

let data = JSON.parse(fs.readFileSync('scratch/cad_block_mm_details.json', 'utf8'));
if (typeof data === 'string') data = JSON.parse(data);
const lines = data.samples.filter(s => s.type === 'Line' && s.layer === 'MONG');

console.log('Total MONG lines in mm:', lines.length);

const baseLines = lines.filter(l => Math.abs(l.len - 1600) < 50);
const topLines = lines.filter(l => Math.abs(l.len - 600) < 50);
const slantLines = lines.filter(l => Math.abs(l.len - 766.5) < 50 || Math.abs(l.len - 800) < 100);

console.log('Lines of len ~ 1600 (wide base):', baseLines.length);
console.log('Lines of len ~ 600 (narrow top):', topLines.length);
console.log('Lines of slant ~ 766.5:', slantLines.length);

console.log('\nTop 5 slant lines:');
slantLines.slice(0, 5).forEach(l => console.log(JSON.stringify(l)));
