const fs = require('fs');

const pileDoc = JSON.parse(fs.readFileSync('.agents/teamwork/worker_m3_foundation/exact_196_pile_mapping.json', 'utf8'));

// Group piles by footing_id
const pilesByFt = {};
pileDoc.forEach(p => {
  const fid = p.footing_id.replace('DC-', 'ĐC-');
  if (!pilesByFt[fid]) pilesByFt[fid] = [];
  pilesByFt[fid].push({ x: p.x, y: p.y, idx: p.pile_idx });
});

console.log('--- 17 THREE-PILE FOOTINGS ORIENTATION ANALYSIS ---');
for (const fid in pilesByFt) {
  const piles = pilesByFt[fid];
  if (piles.length === 3) {
    // 3 piles: find the 2 piles that are aligned (either same X or same Y)
    // and the 3rd pile that forms the triangle apex
    const [p1, p2, p3] = piles;
    // Check if 2 piles have same Y (horizontal base) or same X (vertical base)
    let orientation = '';
    let baseCenter = { x: 0, y: 0 };
    let apex = { x: 0, y: 0 };
    let baseWidth = 0;
    let height = 0;

    // Pairs: (p1, p2), (p2, p3), (p1, p3)
    const pairs = [
      { a: p1, b: p2, c: p3 },
      { a: p2, b: p3, c: p1 },
      { a: p1, b: p3, c: p2 }
    ];

    for (const { a, b, c } of pairs) {
      if (Math.abs(a.y - b.y) < 10) { // Horizontal base
        baseCenter = { x: (a.x + b.x) / 2, y: a.y };
        apex = c;
        baseWidth = Math.abs(a.x - b.x);
        height = Math.abs(c.y - a.y);
        orientation = c.y > a.y ? 'UP' : 'DOWN';
        break;
      } else if (Math.abs(a.x - b.x) < 10) { // Vertical base
        baseCenter = { x: a.x, y: (a.y + b.y) / 2 };
        apex = c;
        baseWidth = Math.abs(a.y - b.y);
        height = Math.abs(c.x - a.x);
        orientation = c.x > a.x ? 'RIGHT' : 'LEFT';
        break;
      }
    }

    console.log(`${fid}: Direction=${orientation}, BaseWidth=${baseWidth}, Height=${height}, BaseCenter=(${baseCenter.x}, ${baseCenter.y}), Apex=(${apex.x}, ${apex.y})`);
  }
}
