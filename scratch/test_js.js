const fs = require('fs');
const html = fs.readFileSync('scratch/story1_beams_3d_simulation.html', 'utf8');
const sIdx = html.indexOf('<script>');
const eIdx = html.lastIndexOf('</script>');
const js = html.substring(sIdx + '<script>'.length, eIdx);
console.log('Script length:', js.length);
try {
  new Function(js);
  console.log('Syntax OK!');
} catch (e) {
  console.error('Syntax Error:', e);
}
