import { TAU, hash01 } from "./bioShapes.js";
const pt = new Float64Array(2);
const ROD_L = 1.45;
const ROD_W = 0.5;
function rodPath(ctx, L, W, bulge = 0.06) {
  ctx.beginPath();
  ctx.moveTo(-L + W, -W);
  ctx.quadraticCurveTo(0, -W * (1 + 2 * bulge), L - W, -W);
  ctx.arc(L - W, 0, W, -Math.PI / 2, Math.PI / 2);
  ctx.quadraticCurveTo(0, W * (1 + 2 * bulge), -L + W, W);
  ctx.arc(-L + W, 0, W, Math.PI / 2, 3 * Math.PI / 2);
  ctx.closePath();
}
const FLAG_N = 5;
const FLAG_SEGS = 14;
function flagellaTuftPath(ctx, r, L, time, phase, seed) {
  ctx.beginPath();
  for (let i = 0; i < FLAG_N; i++) {
    const f = (i - (FLAG_N - 1) / 2) / ((FLAG_N - 1) / 2);
    const ang = Math.PI + f * 0.42 + 0.07 * Math.sin(time * 1.3 + phase + i * 2.3);
    const len = (2.3 + 0.7 * hash01(seed * 8 + i, 90)) * r;
    const dx = Math.cos(ang);
    const dy = Math.sin(ang);
    const x0 = -L + 0.2 * r;
    const y0 = f * 0.14 * r;
    ctx.moveTo(x0, y0);
    for (let k = 1; k <= FLAG_SEGS; k++) {
      const s = k / FLAG_SEGS;
      const w = Math.sin(s * 9.5 - time * 12 + phase + i * 1.7) * 0.16 * r * (0.2 + 0.8 * s);
      ctx.lineTo(x0 + dx * s * len - dy * w, y0 + dy * s * len + dx * w);
    }
  }
}
const SPIRO_SEGS = 40;
const SPIRO_L = 2;
const SPIRO_A = 0.3;
const SPIRO_TURNS = 2.5;
const SPIRO_W = 0.24;
const spiroBuf = new Float64Array(2 * (SPIRO_SEGS + 1));
function spiroCenter(out, s, r, time, phase) {
  const taper = 0.55 + 0.45 * Math.sin(s * Math.PI);
  out[0] = (-SPIRO_L + s * 2 * SPIRO_L) * r;
  out[1] = Math.sin(s * SPIRO_TURNS * TAU - time * 13 + phase) * SPIRO_A * r * taper;
}
function spiroHalfWidth(s, r) {
  return SPIRO_W * r * Math.min(1, 1.15 * Math.pow(Math.sin(Math.PI * s), 0.45));
}
function spiroBodyPath(ctx, r, time, phase) {
  const N = SPIRO_SEGS;
  for (let k = 0; k <= N; k++) {
    spiroCenter(pt, k / N, r, time, phase);
    spiroBuf[2 * k] = pt[0];
    spiroBuf[2 * k + 1] = pt[1];
  }
  ctx.beginPath();
  for (let side = 1; side >= -1; side -= 2) {
    for (let j = 0; j <= N; j++) {
      const k = side > 0 ? j : N - j;
      const k0 = Math.max(0, k - 1);
      const k1 = Math.min(N, k + 1);
      let tx = spiroBuf[2 * k1] - spiroBuf[2 * k0];
      let ty = spiroBuf[2 * k1 + 1] - spiroBuf[2 * k0 + 1];
      const tl = Math.hypot(tx, ty) || 1;
      tx /= tl;
      ty /= tl;
      const w = spiroHalfWidth(k / N, r) * side;
      const x = spiroBuf[2 * k] - ty * w;
      const y = spiroBuf[2 * k + 1] + tx * w;
      if (side > 0 && j === 0) ctx.moveTo(x, y);
      else ctx.lineTo(x, y);
    }
  }
  ctx.closePath();
}
function spiroAxisPath(ctx, r, time, phase, dx, dy, s0, s1) {
  ctx.beginPath();
  const k0 = Math.round(s0 * SPIRO_SEGS);
  const k1 = Math.round(s1 * SPIRO_SEGS);
  for (let k = k0; k <= k1; k++) {
    spiroCenter(pt, k / SPIRO_SEGS, r, time, phase);
    if (k === k0) ctx.moveTo(pt[0] + dx, pt[1] + dy);
    else ctx.lineTo(pt[0] + dx, pt[1] + dy);
  }
}
function spiroGranulesPath(ctx, r, time, phase, every, size) {
  ctx.beginPath();
  for (let k = every >> 1; k < SPIRO_SEGS; k += every) {
    spiroCenter(pt, k / SPIRO_SEGS, r, time, phase);
    ctx.moveTo(pt[0] + size, pt[1]);
    ctx.arc(pt[0], pt[1], size, 0, TAU);
  }
}
const COCCI = [
  { x: 0.28, y: -0.58, s: 0.29 },
  { x: -0.36, y: -0.54, s: 0.3 },
  { x: 0.72, y: -0.42, s: 0.22 },
  { x: 0.02, y: -0.05, s: 0.36 },
  { x: -0.62, y: 0.02, s: 0.3 },
  { x: 0.58, y: 0.12, s: 0.31 },
  { x: -0.74, y: 0.48, s: 0.2 },
  { x: -0.3, y: 0.56, s: 0.32 },
  { x: 0.28, y: 0.56, s: 0.3 }
];
const COCCI_DIVIDING = [1, 5, 8];
function cocciPath(ctx, r, pad) {
  ctx.beginPath();
  for (const c of COCCI) {
    const cr = (c.s + pad) * r;
    ctx.moveTo(c.x * r + cr, c.y * r);
    ctx.arc(c.x * r, c.y * r, cr, 0, TAU);
  }
}
const PH = {
  hx: -0.7,
  rx: 0.78,
  ry: 0.6,
  collar: [0.06, 0.2, 0.24],
  sheath: [0.2, 1.14, 0.13],
  plate: [1.12, 1.28, 0.3]
};
const HEAD_EDGE = 0.24;
function headVertex(id, r, out) {
  if (id < 6) {
    const a = id * TAU / 6;
    out[0] = (PH.hx + Math.cos(a) * PH.rx) * r;
    out[1] = Math.sin(a) * PH.ry * r;
  } else {
    out[0] = PH.hx * r;
    out[1] = (id === 6 ? -HEAD_EDGE : HEAD_EDGE) * PH.ry * r;
  }
}
function headHex(ctx, r) {
  for (let i = 0; i < 6; i++) {
    headVertex(i, r, pt);
    if (i === 0) ctx.moveTo(pt[0], pt[1]);
    else ctx.lineTo(pt[0], pt[1]);
  }
  ctx.closePath();
}
function phageHeadPath(ctx, r) {
  ctx.beginPath();
  headHex(ctx, r);
}
const FACETS = [
  [
    [6, 4, 5],
    [6, 4, 3],
    [6, 5, 0]
  ],
  [
    [6, 7, 3],
    [6, 7, 0]
  ],
  [
    [7, 1, 2],
    [7, 2, 3],
    [7, 1, 0]
  ]
];
function phageFacetsPath(ctx, r, group) {
  ctx.beginPath();
  for (const tri of FACETS[group]) {
    for (let j = 0; j < 3; j++) {
      headVertex(tri[j], r, pt);
      if (j === 0) ctx.moveTo(pt[0], pt[1]);
      else ctx.lineTo(pt[0], pt[1]);
    }
    ctx.closePath();
  }
}
const HEAD_EDGES = [
  [6, 7],
  [6, 4],
  [6, 5],
  [6, 0],
  [6, 3],
  [7, 1],
  [7, 2],
  [7, 0],
  [7, 3]
];
function phageEdgesPath(ctx, r) {
  ctx.beginPath();
  for (const [a, b] of HEAD_EDGES) {
    headVertex(a, r, pt);
    ctx.moveTo(pt[0], pt[1]);
    headVertex(b, r, pt);
    ctx.lineTo(pt[0], pt[1]);
  }
}
function segRect(ctx, r, seg) {
  ctx.rect(seg[0] * r, -seg[2] * r, (seg[1] - seg[0]) * r, 2 * seg[2] * r);
}
function phageCollarPath(ctx, r) {
  ctx.beginPath();
  segRect(ctx, r, PH.collar);
}
function phageSheathPath(ctx, r) {
  ctx.beginPath();
  segRect(ctx, r, PH.sheath);
}
function phagePlatePath(ctx, r) {
  ctx.beginPath();
  segRect(ctx, r, PH.plate);
}
function phageSilhouettePath(ctx, r) {
  ctx.beginPath();
  headHex(ctx, r);
  segRect(ctx, r, PH.collar);
  segRect(ctx, r, PH.sheath);
  segRect(ctx, r, PH.plate);
}
function phageStriationsPath(ctx, r) {
  ctx.beginPath();
  const [x0, x1, h] = PH.sheath;
  for (let k = 0; k < 9; k++) {
    const x = (x0 + (k + 0.5) / 9 * (x1 - x0)) * r;
    ctx.moveTo(x, -h * r);
    ctx.lineTo(x, h * r);
  }
}
function phageWhiskersPath(ctx, r) {
  ctx.beginPath();
  for (let side = -1; side <= 1; side += 2) {
    ctx.moveTo((PH.collar[0] + 0.04) * r, side * PH.collar[2] * r);
    ctx.lineTo(-0.14 * r, side * 0.46 * r);
  }
}
function phagePinsPath(ctx, r) {
  ctx.beginPath();
  const x = PH.plate[1] * r;
  for (let k = -1; k <= 1; k++) {
    ctx.moveTo(x, k * 0.19 * r);
    ctx.lineTo(x + 0.1 * r, k * 0.22 * r);
  }
}
const FIBERS_PER_SIDE = 3;
function phageFibersPath(ctx, r, time, phase, spread) {
  ctx.beginPath();
  const x0 = (PH.plate[1] - 0.04) * r;
  for (let side = -1; side <= 1; side += 2) {
    for (let i = 0; i < FIBERS_PER_SIDE; i++) {
      const twitch = Math.sin(time * 7 + phase + i * 1.9 + side) * 0.04 * r;
      ctx.moveTo(x0, side * (0.12 + 0.09 * i) * r);
      ctx.lineTo(1.32 * r, side * (0.5 + 0.22 * i) * r * spread + twitch);
      ctx.lineTo(2 * r, side * (0.28 + 0.3 * i) * r * spread + twitch * 1.5);
    }
  }
}
function phageFiberTipsPath(ctx, r, time, phase, spread, size) {
  ctx.beginPath();
  for (let side = -1; side <= 1; side += 2) {
    for (let i = 0; i < FIBERS_PER_SIDE; i++) {
      const twitch = Math.sin(time * 7 + phase + i * 1.9 + side) * 0.04 * r;
      const y = side * (0.28 + 0.3 * i) * r * spread + twitch * 1.5;
      ctx.moveTo(2 * r + size, y);
      ctx.arc(2 * r, y, size, 0, TAU);
    }
  }
}
const GENOME_SEGS = 28;
const GENOME_TURNS = 1.6;
function genomePoint(out, s, r, rot) {
  const a = rot + s * GENOME_TURNS * TAU;
  const d = 0.08 + 0.36 * s;
  out[0] = (PH.hx + Math.cos(a) * d * PH.rx * 1.9) * r;
  out[1] = Math.sin(a) * d * PH.ry * 1.9 * r;
}
function phageGenomePath(ctx, r, rot) {
  ctx.beginPath();
  for (let k = 0; k <= GENOME_SEGS; k++) {
    genomePoint(pt, k / GENOME_SEGS, r, rot);
    if (k === 0) ctx.moveTo(pt[0], pt[1]);
    else ctx.lineTo(pt[0], pt[1]);
  }
}
function phageGenomeDotsPath(ctx, r, rot, size) {
  ctx.beginPath();
  for (let k = 0; k < 9; k++) {
    genomePoint(pt, (k + 0.5) / 9, r, rot);
    ctx.moveTo(pt[0] + size, pt[1]);
    ctx.arc(pt[0], pt[1], size, 0, TAU);
  }
}
const CILIATE_SAMPLES = 36;
const CILIATE_RX = 1.25;
const CILIATE_RY = 0.72;
const GROOVE_ANG = -0.35 * Math.PI;
const GROOVE_W = 0.35;
const GROOVE_DEPTH = 0.12;
function ciliateOutline(out, th, r, time, phase) {
  const breathe = 1 + 0.03 * Math.sin(time * 1.1 + phase);
  const cx = Math.cos(th);
  const sy = Math.sin(th);
  const ry = CILIATE_RY * (1 - 0.12 * -cx);
  let dth = th - GROOVE_ANG;
  dth = Math.atan2(Math.sin(dth), Math.cos(dth));
  const dent = GROOVE_DEPTH * Math.exp(-(dth * dth) / (2 * GROOVE_W * GROOVE_W));
  const k = breathe - dent;
  out[0] = cx * CILIATE_RX * r * k;
  out[1] = sy * ry * r * k;
}
const ciliateBuf = new Float64Array(2 * CILIATE_SAMPLES);
function ciliateBodyPath(ctx, r, time, phase) {
  const N = CILIATE_SAMPLES;
  for (let i = 0; i < N; i++) {
    ciliateOutline(pt, i / N * TAU, r, time, phase);
    ciliateBuf[2 * i] = pt[0];
    ciliateBuf[2 * i + 1] = pt[1];
  }
  ctx.beginPath();
  const lx = ciliateBuf[2 * (N - 1)];
  const ly = ciliateBuf[2 * (N - 1) + 1];
  ctx.moveTo((lx + ciliateBuf[0]) / 2, (ly + ciliateBuf[1]) / 2);
  for (let i = 0; i < N; i++) {
    const x = ciliateBuf[2 * i];
    const y = ciliateBuf[2 * i + 1];
    const j = (i + 1) % N;
    ctx.quadraticCurveTo(x, y, (x + ciliateBuf[2 * j]) / 2, (y + ciliateBuf[2 * j + 1]) / 2);
  }
  ctx.closePath();
}
function ciliaPath(ctx, r, time, phase, N) {
  ctx.beginPath();
  const len = 0.2 * r;
  const bend = 0.11 * r;
  for (let k = 0; k < N; k++) {
    const th = k / N * TAU;
    ciliateOutline(pt, th, r, time, phase);
    const x = pt[0];
    const y = pt[1];
    let nx = Math.cos(th) / CILIATE_RX;
    let ny = Math.sin(th) / CILIATE_RY;
    const nl = Math.hypot(nx, ny) || 1;
    nx /= nl;
    ny /= nl;
    const w = Math.sin(k * 0.55 - time * 12 + phase) * bend;
    ctx.moveTo(x, y);
    ctx.lineTo(x + nx * len - ny * w, y + ny * len + nx * w);
  }
}
function ciliateRindPath(ctx, r, time, phase, N) {
  ctx.beginPath();
  for (let k = 0; k < N; k++) {
    ciliateOutline(pt, (k + 0.5) / N * TAU, r, time, phase);
    ctx.moveTo(pt[0] * 0.87, pt[1] * 0.87);
    ctx.lineTo(pt[0] * 0.96, pt[1] * 0.96);
  }
}
const GROOVE_STEPS = 8;
function ciliateGrooveWedgePath(ctx, r, time, phase) {
  ctx.beginPath();
  for (let k = 0; k <= GROOVE_STEPS; k++) {
    const th = GROOVE_ANG + (k / GROOVE_STEPS - 0.5) * 3.2 * GROOVE_W;
    ciliateOutline(pt, th, r, time, phase);
    if (k === 0) ctx.moveTo(pt[0] * 0.97, pt[1] * 0.97);
    else ctx.lineTo(pt[0] * 0.97, pt[1] * 0.97);
  }
  for (let k = GROOVE_STEPS; k >= 0; k--) {
    const th = GROOVE_ANG + (k / GROOVE_STEPS - 0.5) * 1.6 * GROOVE_W;
    ciliateOutline(pt, th, r, time, phase);
    ctx.lineTo(pt[0] * 0.6, pt[1] * 0.6);
  }
  ctx.closePath();
}
function ciliateGrooveLinePath(ctx, r, time, phase) {
  ctx.beginPath();
  for (let k = 0; k <= GROOVE_STEPS; k++) {
    const th = GROOVE_ANG + (k / GROOVE_STEPS - 0.5) * 2.2 * GROOVE_W;
    ciliateOutline(pt, th, r, time, phase);
    const f = 0.62 + 0.28 * Math.abs(k / GROOVE_STEPS - 0.5) * 2;
    if (k === 0) ctx.moveTo(pt[0] * f, pt[1] * f);
    else ctx.lineTo(pt[0] * f, pt[1] * f);
  }
}
function ciliateCytostome(out, r, time, phase) {
  ciliateOutline(out, GROOVE_ANG, r, time, phase);
  out[0] = out[0] * 0.5;
  out[1] = out[1] * 0.5;
}
function canalsPath(ctx, x, y, rays, r0, len, rot) {
  ctx.beginPath();
  for (let k = 0; k < rays; k++) {
    const a = rot + k / rays * TAU;
    ctx.moveTo(x + Math.cos(a) * r0, y + Math.sin(a) * r0);
    ctx.lineTo(x + Math.cos(a) * len, y + Math.sin(a) * len);
  }
}
function canalBulbsPath(ctx, x, y, rays, len, rot, size) {
  ctx.beginPath();
  for (let k = 0; k < rays; k++) {
    const a = rot + k / rays * TAU;
    const bx = x + Math.cos(a) * len;
    const by = y + Math.sin(a) * len;
    ctx.moveTo(bx + size, by);
    ctx.arc(bx, by, size, 0, TAU);
  }
}
export {
  CILIATE_RX,
  CILIATE_RY,
  COCCI,
  COCCI_DIVIDING,
  PH,
  ROD_L,
  ROD_W,
  canalBulbsPath,
  canalsPath,
  ciliaPath,
  ciliateBodyPath,
  ciliateCytostome,
  ciliateGrooveLinePath,
  ciliateGrooveWedgePath,
  ciliateRindPath,
  cocciPath,
  flagellaTuftPath,
  phageCollarPath,
  phageEdgesPath,
  phageFacetsPath,
  phageFiberTipsPath,
  phageFibersPath,
  phageGenomeDotsPath,
  phageGenomePath,
  phageHeadPath,
  phagePinsPath,
  phagePlatePath,
  phageSheathPath,
  phageSilhouettePath,
  phageStriationsPath,
  phageWhiskersPath,
  rodPath,
  spiroAxisPath,
  spiroBodyPath,
  spiroGranulesPath
};
