const data = require('./compact_sim_data.json');
console.log('Total beams:', data.beams.length);
console.log('Total cols:', data.cols.length);
console.log('Total walls:', data.walls.length);
console.log('Total grids:', data.grids.length);

const panX = 24450, panY = 13950;
const scale = 0.015; // 50m * 0.015 = 750px
const yaw = 0.5, pitch = 0.6;
const cx = 400, cy = 300;

function project(x, y, z) {
  const dx = x - panX;
  const dy = y - panY;
  const x1 = dx * Math.cos(yaw) - dy * Math.sin(yaw);
  const y1 = dx * Math.sin(yaw) + dy * Math.cos(yaw);
  const y2 = y1 * Math.cos(pitch) - z * Math.sin(pitch);
  return [cx + x1 * scale, cy - y2 * scale];
}

console.log('Center point:', project(24450, 13950, 0));
console.log('Bottom-left (0, 0, 0):', project(0, 0, 0));
console.log('Top-right (48900, 27900, 0):', project(48900, 27900, 0));
