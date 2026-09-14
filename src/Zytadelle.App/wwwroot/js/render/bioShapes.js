const TAU = Math.PI * 2;
function hash01(i, salt) {
  let x = i * 374761393 + salt * 668265263 | 0;
  x = Math.imul(x ^ x >>> 13, 1274126177);
  return ((x ^ x >>> 16) >>> 0) / 4294967296;
}
function makeParticles(n) {
  const out = [];
  for (let i = 0; i < n; i++) {
    out.push({
      ang: hash01(i, 1) * TAU,
      rad: 0.06 + Math.sqrt(hash01(i, 2)) * 0.92,
      // sqrt → uniform over disc area
      size: 0.12 + hash01(i, 3) * 0.45,
      spin: (hash01(i, 4) - 0.5) * 0.03,
      bob: 0.3 + hash01(i, 5) * 0.5,
      phase: hash01(i, 6) * TAU,
      bubble: hash01(i, 7) < 0.15
    });
  }
  return out;
}
function makeGranules(n) {
  const out = [];
  for (let i = 0; i < n; i++) {
    const dir = hash01(i, 14) < 0.5 ? -1 : 1;
    out.push({
      ang: hash01(i, 11) * TAU,
      rad: 0.3 + hash01(i, 12) * 0.5,
      size: 0.14 + hash01(i, 13) * 0.2,
      speed: dir * (0.05 + hash01(i, 15) * 0.1),
      phase: hash01(i, 16) * TAU,
      dark: i % 3 === 0
    });
  }
  return out;
}
const LOBES = [
  { ang: 0.25, amp: 0.26, w: 0.38 },
  { ang: 1.95, amp: 0.34, w: 0.3 },
  { ang: 3.35, amp: 0.2, w: 0.45 },
  { ang: 4.9, amp: 0.3, w: 0.34 }
];
function blobPath(ctx, buf, r, time, wobble, lobeScale, phase) {
  const N = buf.length >> 1;
  const rot = time * 0.12;
  for (let i = 0; i < N; i++) {
    const th = i / N * TAU;
    let k = 1 + wobble * (0.6 * Math.sin(3 * th + time * 1.3 + phase) + 0.4 * Math.sin(5 * th - time * 0.9 + phase * 1.7));
    if (lobeScale > 0) {
      for (const L of LOBES) {
        const d = th - (L.ang + rot);
        const dd = Math.atan2(Math.sin(d), Math.cos(d));
        k += lobeScale * L.amp * Math.exp(-(dd * dd) / (2 * L.w * L.w)) * (0.85 + 0.15 * Math.sin(time * 0.8 + L.ang * 3));
      }
    }
    buf[2 * i] = Math.cos(th) * r * k;
    buf[2 * i + 1] = Math.sin(th) * r * k;
  }
  ctx.beginPath();
  const lx = buf[2 * (N - 1)];
  const ly = buf[2 * (N - 1) + 1];
  ctx.moveTo((lx + buf[0]) / 2, (ly + buf[1]) / 2);
  for (let i = 0; i < N; i++) {
    const x = buf[2 * i];
    const y = buf[2 * i + 1];
    const j = (i + 1) % N;
    const nx = buf[2 * j];
    const ny = buf[2 * j + 1];
    ctx.quadraticCurveTo(x, y, (x + nx) / 2, (y + ny) / 2);
  }
  ctx.closePath();
}
function beanPath(ctx, s) {
  ctx.beginPath();
  ctx.moveTo(-s, 0);
  ctx.bezierCurveTo(-s, -0.78 * s, -0.48 * s, -0.58 * s, 0, -0.34 * s);
  ctx.bezierCurveTo(0.48 * s, -0.58 * s, s, -0.78 * s, s, 0);
  ctx.bezierCurveTo(s, 0.72 * s, 0.42 * s, 0.82 * s, 0, 0.76 * s);
  ctx.bezierCurveTo(-0.42 * s, 0.82 * s, -s, 0.72 * s, -s, 0);
  ctx.closePath();
}
function bacillusPath(ctx, r) {
  const L = 1.25 * r;
  const W = 0.62 * r;
  ctx.beginPath();
  ctx.moveTo(-L + W, -W);
  ctx.lineTo(L - W, -W);
  ctx.arc(L - W, 0, W, -Math.PI / 2, Math.PI / 2);
  ctx.lineTo(-L + W, W);
  ctx.arc(-L + W, 0, W, Math.PI / 2, 3 * Math.PI / 2);
  ctx.closePath();
}
function makeOrganelles(n) {
  const out = [];
  for (let i = 0; i < n; i++) {
    const dir = hash01(i, 41) < 0.5 ? -1 : 1;
    out.push({
      ang: hash01(i, 42) * TAU,
      rad: 0.42 + hash01(i, 43) * 0.36,
      size: 0.19 + hash01(i, 44) * 0.07,
      speed: dir * (0.04 + hash01(i, 45) * 0.06),
      phase: hash01(i, 46) * TAU,
      tilt: hash01(i, 47) * TAU,
      tiltSpeed: (hash01(i, 48) - 0.5) * 0.3
    });
  }
  return out;
}
function mitoCristaePath(ctx, L, W) {
  ctx.beginPath();
  for (let i = -1; i <= 1; i++) {
    const x = i * L * 0.45;
    ctx.moveTo(x - W * 0.25, -W * 0.7);
    ctx.lineTo(x + W * 0.25, W * 0.7);
  }
}
function helixPath(ctx, L, A, segs, turns, phase, strand) {
  ctx.beginPath();
  for (let k = 0; k <= segs; k++) {
    const s = k / segs;
    const x = s * L;
    const y = Math.sin(s * turns * TAU + phase + strand * Math.PI) * A;
    if (k === 0) ctx.moveTo(x, y);
    else ctx.lineTo(x, y);
  }
}
function helixRungsPath(ctx, L, A, segs, turns, phase, every) {
  ctx.beginPath();
  for (let k = 0; k <= segs; k += every) {
    const s = k / segs;
    const x = s * L;
    const y = Math.sin(s * turns * TAU + phase) * A;
    ctx.moveTo(x, y);
    ctx.lineTo(x, -y);
  }
}
function helixNodesPath(ctx, L, A, segs, turns, phase, every, size) {
  ctx.beginPath();
  for (let k = 0; k <= segs; k += every) {
    const s = k / segs;
    const x = s * L;
    const y = Math.sin(s * turns * TAU + phase) * A;
    ctx.moveTo(x + size, y);
    ctx.arc(x, y, size, 0, TAU);
  }
}
export {
  LOBES,
  TAU,
  bacillusPath,
  beanPath,
  blobPath,
  hash01,
  helixNodesPath,
  helixPath,
  helixRungsPath,
  makeGranules,
  makeOrganelles,
  makeParticles,
  mitoCristaePath
};
