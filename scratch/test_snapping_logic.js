const fs = require('fs');
const beams = JSON.parse(fs.readFileSync('.agents/teamwork/worker_m3_foundation/cad_extracted_beam_spans.json', 'utf8'));
const xGrids = [0, 1300, 6900, 9900, 13900, 16500, 23700, 29050, 35750, 38300, 40800, 45000, 48900];
const yGrids = [0, 2800, 4650, 7150, 9950, 12750, 13950, 15150, 17950, 20000, 20750, 23250, 25100, 27900];

const MaxFootingEmbedmentMm = 1350.0;
const MaxAxisDistanceToPrimaryGridMm = 250.0;

function snapStart(val, gridList) {
  let gPrev = null, dPrev = Infinity;
  let gNext = null, dNext = Infinity;
  for (const g of gridList) {
    if (g <= val) {
      if (val - g < dPrev) { dPrev = val - g; gPrev = g; }
    }
    if (g >= val) {
      if (g - val < dNext) { dNext = g - val; gNext = g; }
    }
  }

  if (dNext <= 50.0) return gNext;
  if (dPrev <= MaxFootingEmbedmentMm && gPrev !== null) return gPrev;
  if (dNext <= 250.0) return gNext;
  return val;
}

function snapEnd(val, gridList) {
  let gPrev = null, dPrev = Infinity;
  let gNext = null, dNext = Infinity;
  for (const g of gridList) {
    if (g <= val) {
      if (val - g < dPrev) { dPrev = val - g; gPrev = g; }
    }
    if (g >= val) {
      if (g - val < dNext) { dNext = g - val; gNext = g; }
    }
  }

  if (dPrev <= 50.0) return gPrev;
  if (dNext <= MaxFootingEmbedmentMm && gNext !== null) return gNext;
  if (dPrev <= 250.0) return gPrev;
  return val;
}

function findNearest(val, list) {
  let minD = Infinity, best = val;
  for (const x of list) {
    const d = Math.abs(val - x);
    if (d < minD) { minD = d; best = x; }
  }
  return { target: best, dist: minD };
}

console.log('Testing outward grid intersection snapping:');
let total = 0, snappedBoth = 0;
beams.forEach(b => {
  total++;
  const isH = Math.abs(b.y1 - b.y2) <= 50;
  if (isH) {
    const s1 = Math.min(b.x1, b.x2);
    const s2 = Math.max(b.x1, b.x2);
    const snap1 = snapStart(s1, xGrids);
    const snap2 = snapEnd(s2, xGrids);
    if (xGrids.includes(snap1) && xGrids.includes(snap2)) snappedBoth++;
  } else {
    const s1 = Math.min(b.y1, b.y2);
    const s2 = Math.max(b.y1, b.y2);
    const snap1 = snapStart(s1, yGrids);
    const snap2 = snapEnd(s2, yGrids);
    if (yGrids.includes(snap1) && yGrids.includes(snap2)) snappedBoth++;
  }
});

console.log(`Total beams: ${total}, Snapped both ends to grids: ${snappedBoth} (${(snappedBoth/total*100).toFixed(1)}%)`);

// Check GM-18a specifically:
const gm18a = beams.filter(b => b.name === 'GM-18a');
console.log('\nGM-18a results:');
gm18a.forEach(b => {
  const s1 = Math.min(b.y1, b.y2);
  const s2 = Math.max(b.y1, b.y2);
  const snap1 = snapStart(s1, yGrids);
  const snap2 = snapEnd(s2, yGrids);
  console.log(`${b.name}: raw (${s1} -> ${s2}, len=${s2-s1}) => snapped (${snap1} -> ${snap2}, len=${snap2-snap1})`);
});
