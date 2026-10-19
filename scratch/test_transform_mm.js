const fs = require('fs');

const worldLines = JSON.parse(fs.readFileSync('scratch/cad_transformed_mong_lines.json', 'utf8'));
console.log('Total world lines on layer MONG:', worldLines.length);

// Let's inspect lines around DC-3-01 (centerX ~ 6850, centerY ~ -133)
const dc301Lines = worldLines.filter(l => 
  Math.abs(l.x1 - 6850) < 1500 && Math.abs(l.y1 - (-133)) < 1500 &&
  Math.abs(l.x2 - 6850) < 1500 && Math.abs(l.y2 - (-133)) < 1500
);

console.log('Lines near DC-3-01:');
dc301Lines.forEach(l => console.log(JSON.stringify(l)));
