const fs = require('fs');
const html = fs.readFileSync('scratch/story1_beams_3d_simulation.html', 'utf8');

const sIdx = html.indexOf('const RAW_BEAMS =');
const eIdx = html.indexOf('const canvas =');
const dataJs = html.substring(sIdx, eIdx);
eval(dataJs);

console.log('RAW_BEAMS:', Array.isArray(RAW_BEAMS.beams) ? RAW_BEAMS.beams.length : 'not array');
console.log('RAW_CAD_LINES xLines:', RAW_CAD_LINES.xLines?.length, 'yLines:', RAW_CAD_LINES.yLines?.length);
console.log('RAW_COLUMNS:', RAW_COLUMNS.columns?.length);
console.log('RAW_WALLS:', RAW_WALLS.walls?.length);
console.log('RAW_GRIDS:', RAW_GRIDS.xGrids?.length, RAW_GRIDS.yGrids?.length);
