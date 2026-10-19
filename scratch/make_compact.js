const fs = require('fs');
const beamsData = JSON.parse(fs.readFileSync('scratch/story1_grade_beams_model.json', 'utf8'));
const colsData = JSON.parse(fs.readFileSync('scratch/columns_story1_model.json', 'utf8'));
const wallsData = JSON.parse(fs.readFileSync('scratch/elevator_walls_model.json', 'utf8'));
const gridsData = JSON.parse(fs.readFileSync('scratch/revit_grids_and_levels.json', 'utf8'));

const compactBeams = beamsData.beams.map(b => [
  Math.round(b.StartX), Math.round(b.StartY),
  Math.round(b.EndX), Math.round(b.EndY),
  b.WidthMm, b.HeightMm,
  b.Axis, b.Mark || b.Name, b.TypeName
]);

const compactCols = colsData.columns.map(c => [
  Math.round(c.xMm), Math.round(c.yMm), c.widthMm, c.heightMm
]);

const compactWalls = wallsData.walls.map(w => [
  Math.round(w.x0Mm), Math.round(w.y0Mm), Math.round(w.x1Mm), Math.round(w.y1Mm), w.thicknessMm
]);

const compactGrids = gridsData.grids.map(g => [
  g.name, Math.round(g.x0), Math.round(g.y0), Math.round(g.x1), Math.round(g.y1)
]);

const payload = {
  beams: compactBeams,
  cols: compactCols,
  walls: compactWalls,
  grids: compactGrids
};

const json = JSON.stringify(payload);
console.log('Compact JSON size (bytes):', json.length);
fs.writeFileSync('scratch/compact_sim_data.json', json, 'utf8');
