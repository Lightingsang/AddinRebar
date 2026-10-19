const fs = require('fs');
const beams = JSON.parse(fs.readFileSync('.agents/teamwork/worker_m3_foundation/cad_extracted_beam_spans.json', 'utf8'));
const xGrids = [0, 1300, 6900, 9900, 13900, 16500, 23700, 29050, 35750, 38300, 40800, 45000, 48900];
const yGrids = [0, 2800, 4650, 7150, 9950, 12750, 13950, 15150, 17950, 20000, 20750, 23250, 25100, 27900];

function findNearest(val, list) {
  let minD = Infinity, best = val;
  for (const x of list) {
    const d = Math.abs(val - x);
    if (d < minD) { minD = d; best = x; }
  }
  return { target: best, dist: minD };
}

console.log('--- BEAMS WHOSE ENDPOINTS DO NOT COINCIDE WITH GRIDS ---');
let count = 0;
beams.forEach((b, idx) => {
  const isH = Math.abs(b.y1 - b.y2) <= 50;
  if (isH) {
    const s1 = findNearest(b.x1, xGrids);
    const s2 = findNearest(b.x2, xGrids);
    if (s1.dist > 50 || s2.dist > 50) {
      count++;
      console.log(`[H] ${b.name} (${b.x1},${b.y1})->(${b.x2},${b.y2}): len=${b.len}, dX1=${s1.dist.toFixed(1)} to ${s1.target}, dX2=${s2.dist.toFixed(1)} to ${s2.target}`);
    }
  } else {
    const s1 = findNearest(b.y1, yGrids);
    const s2 = findNearest(b.y2, yGrids);
    if (s1.dist > 50 || s2.dist > 50) {
      count++;
      console.log(`[V] ${b.name} (${b.x1},${b.y1})->(${b.x2},${b.y2}): len=${b.len}, dY1=${s1.dist.toFixed(1)} to ${s1.target}, dY2=${s2.dist.toFixed(1)} to ${s2.target}`);
    }
  }
});
console.log('Total non-snapped / clear-span beams:', count);
