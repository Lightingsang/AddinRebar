const fs = require('fs');
const html = fs.readFileSync('scratch/story1_beams_3d_simulation.html', 'utf8');
const sIdx = html.indexOf('const RAW_BEAMS =');
const eIdx = html.indexOf('const vbo = gl.createBuffer();');
const dataJs = html.substring(sIdx, eIdx).replace(/const /g, 'var ').replace(/let /g, 'var ');
// Mock DOM
var glMock = {
  createShader: () => ({}),
  shaderSource: () => {},
  compileShader: () => {},
  createProgram: () => ({}),
  attachShader: () => {},
  linkProgram: () => {},
  useProgram: () => {},
  getAttribLocation: () => 0,
  getUniformLocation: () => 0,
  enable: () => {},
  blendFunc: () => {}, viewport: () => {}
};
var document = {
  getElementById: function(id) {
    if (id === 'selSection') return { value: 'ALL' };
    if (id === 'canvas3d') return canvas;
    return { textContent: '' };
  }
};
var window = {
  innerWidth: 1920,
  innerHeight: 1080,
  devicePixelRatio: 1,
  addEventListener: () => {}
};
var canvas = {
  width: 1920,
  height: 1080,
  getContext: () => glMock
};
var selectedBeamIndex = 0;
eval(dataJs);

console.log('beamVerts floats:', beamVerts.length, 'triangles:', beamVerts.length / 9 / 3);
console.log('cadVerts floats:', cadVerts.length, 'lines:', cadVerts.length / 9 / 2);
console.log('colVerts floats:', colVerts.length, 'triangles:', colVerts.length / 9 / 3);
