const yaw = 0.55, pitch = 0.75;
let zoom = 0.016;
let panX = 24450, panY = 13950;
const cx = 500, cy = 260;

function project(x, y, z) {
  const dx = x - panX;
  const dy = y - panY;
  const x1 = dx * Math.cos(yaw) - dy * Math.sin(yaw);
  const y1 = dx * Math.sin(yaw) + dy * Math.cos(yaw);
  const y2 = y1 * Math.cos(pitch) - z * Math.sin(pitch);
  return [cx + x1 * zoom, cy - y2 * zoom];
}

function unproject(u, v, z = 0) {
  const x1 = (u - cx) / zoom;
  const y2 = (cy - v) / zoom;
  const cp = Math.max(0.001, Math.cos(pitch));
  const y1 = (y2 + z * Math.sin(pitch)) / cp;
  const dx = x1 * Math.cos(yaw) + y1 * Math.sin(yaw);
  const dy = -x1 * Math.sin(yaw) + y1 * Math.cos(yaw);
  return [panX + dx, panY + dy];
}

// Test cursor at (650, 200)
const cursorScreen = [650, 200];
const worldUnderCursor = unproject(cursorScreen[0], cursorScreen[1]);
console.log('World under cursor:', worldUnderCursor);

// Verify round-trip projection:
const pCheck = project(worldUnderCursor[0], worldUnderCursor[1], 0);
console.log('Projected back to screen:', pCheck, 'Diff:', Math.hypot(pCheck[0] - cursorScreen[0], pCheck[1] - cursorScreen[1]));

// Zoom in by factor 1.2
const k = 1.2;
zoom *= k;
panX = worldUnderCursor[0] - (worldUnderCursor[0] - panX) / k;
panY = worldUnderCursor[1] - (worldUnderCursor[1] - panY) / k;

// Check if world point still projects to (650, 200) after zoom
const pAfterZoom = project(worldUnderCursor[0], worldUnderCursor[1], 0);
console.log('After zoom project:', pAfterZoom, 'Diff:', Math.hypot(pAfterZoom[0] - cursorScreen[0], pAfterZoom[1] - cursorScreen[1]));
