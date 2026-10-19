const fs = require('fs');
const html = fs.readFileSync('scratch/story1_beams_3d_simulation.html', 'utf8');

function mat4Perspective(out, fov, aspect, near, far) {
  const f = 1.0 / Math.tan(fov / 2);
  const nf = 1 / (near - far);
  out[0] = f / aspect; out[1] = 0; out[2] = 0; out[3] = 0;
  out[4] = 0; out[5] = f; out[6] = 0; out[7] = 0;
  out[8] = 0; out[9] = 0; out[10] = (far + near) * nf; out[11] = -1;
  out[12] = 0; out[13] = 0; out[14] = (2 * far * near) * nf; out[15] = 0;
}

function mat4Multiply(out, a, b) {
  const a00 = a[0], a01 = a[1], a02 = a[2], a03 = a[3];
  const a10 = a[4], a11 = a[5], a12 = a[6], a13 = a[7];
  const a20 = a[8], a21 = a[9], a22 = a[10], a23 = a[11];
  const a30 = a[12], a31 = a[13], a32 = a[14], a33 = a[15];

  let b0 = b[0], b1 = b[1], b2 = b[2], b3 = b[3];
  out[0] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
  out[1] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
  out[2] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
  out[3] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

  b0 = b[4]; b1 = b[5]; b2 = b[6]; b3 = b[7];
  out[4] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
  out[5] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
  out[6] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
  out[7] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

  b0 = b[8]; b1 = b[9]; b2 = b[10]; b3 = b[11];
  out[8] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
  out[9] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
  out[10] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
  out[11] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

  b0 = b[12]; b1 = b[13]; b2 = b[14]; b3 = b[15];
  out[12] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
  out[13] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
  out[14] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
  out[15] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;
}

const rotX = 0.85, rotY = 0.55;
const camDist = 65000;
const panX = 24450, panY = 13950;
const aspect = 1.77;

const pMat = new Float32Array(16);
mat4Perspective(pMat, Math.PI / 4, aspect, 100, 200000);

const vMat = new Float32Array([1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,-camDist,1]);
const rxMat = new Float32Array([1,0,0,0, 0,Math.cos(rotX),Math.sin(rotX),0, 0,-Math.sin(rotX),Math.cos(rotX),0, 0,0,0,1]);
const rzMat = new Float32Array([Math.cos(rotY),Math.sin(rotY),0,0, -Math.sin(rotY),Math.cos(rotY),0,0, 0,0,1,0, 0,0,0,1]);
const tMat = new Float32Array([1,0,0,0, 0,1,0,0, 0,0,1,0, -panX, -panY, 0, 1]);

const tmp1 = new Float32Array(16);
const tmp2 = new Float32Array(16);
const view = new Float32Array(16);
const mvp = new Float32Array(16);

mat4Multiply(tmp1, vMat, rxMat);
mat4Multiply(tmp2, tmp1, rzMat);
mat4Multiply(view, tmp2, tMat);
mat4Multiply(mvp, pMat, view);

// Test vertex at center: (24450, 13950, -100)
const v = [24450, 13950, -100, 1];
const clip = [0, 0, 0, 0];
for (let r = 0; r < 4; r++) {
  clip[r] = mvp[0*4 + r]*v[0] + mvp[1*4 + r]*v[1] + mvp[2*4 + r]*v[2] + mvp[3*4 + r]*v[3];
}
const ndc = [clip[0]/clip[3], clip[1]/clip[3], clip[2]/clip[3]];
console.log('Clip:', clip);
console.log('NDC:', ndc);
const testPoints = [
  [0, 0, 0],
  [48900, 0, 0],
  [0, 27900, 0],
  [48900, 27900, 0],
  [24450, 13950, 0]
];

for (const tp of testPoints) {
  const v = [tp[0], tp[1], tp[2], 1];
  const clip = [0, 0, 0, 0];
  for (let r = 0; r < 4; r++) {
    clip[r] = mvp[0*4 + r]*v[0] + mvp[1*4 + r]*v[1] + mvp[2*4 + r]*v[2] + mvp[3*4 + r]*v[3];
  }
  const ndc = [clip[0]/clip[3], clip[1]/clip[3], clip[2]/clip[3]];
  console.log(tp, 'NDC:', ndc, 'In Frustum?', ndc[0] >= -1 && ndc[0] <= 1 && ndc[1] >= -1 && ndc[1] <= 1 && ndc[2] >= -1 && ndc[2] <= 1);
}
