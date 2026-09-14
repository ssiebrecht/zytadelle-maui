import { ENEMIES } from "./constants.js";
import { TAU, beanPath, hash01 } from "./bioShapes.js";
import {
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
} from "./pathogenShapes.js";
import { P, glowStops, hexRgb, linear, radial, rgba, shadeRgb, tintRgb } from "./palette.js";
const KINDS = Object.keys(ENEMIES);
const BLOOM_R = { basic: 2, fast: 1.9, tank: 1.8, ranged: 2.1, boss: 2.5 };
const LOD_CROWD = 60;
const LX = -Math.SQRT1_2;
const LY = -Math.SQRT1_2;
function lit(ctx, grad, rot, s, tx = 0, ty = 0) {
  ctx.save();
  ctx.translate(tx, ty);
  ctx.rotate(-rot);
  ctx.scale(s, s);
  ctx.fillStyle = grad;
  ctx.fill();
  ctx.restore();
}
function local(ctx, grad, sx, sy, tx = 0, ty = 0) {
  ctx.save();
  ctx.translate(tx, ty);
  ctx.scale(sx, sy);
  ctx.fillStyle = grad;
  ctx.fill();
  ctx.restore();
}
function toLocal(rot, sx, sy, out) {
  const c = Math.cos(rot);
  const s = Math.sin(rot);
  out[0] = sx * c + sy * s;
  out[1] = -sx * s + sy * c;
}
function wall(ctx, kit, lw) {
  ctx.lineWidth = lw;
  ctx.strokeStyle = `rgba(${kit.deep},0.85)`;
  ctx.stroke();
  ctx.lineWidth = lw * 0.4;
  ctx.strokeStyle = `rgba(${kit.light},0.6)`;
  ctx.stroke();
}
const gPt = new Float64Array(2);
const lPt = new Float64Array(2);
function foam(ctx, kit, n, size, place) {
  ctx.fillStyle = "rgba(0,0,0,0.22)";
  ctx.beginPath();
  for (let i = 0; i < n; i += 3) {
    place(i, gPt);
    const s = size * (0.9 + 0.7 * hash01(i, 98));
    ctx.moveTo(gPt[0] + s, gPt[1]);
    ctx.arc(gPt[0], gPt[1], s, 0, TAU);
  }
  ctx.fill();
  ctx.fillStyle = `rgba(${kit.light},0.42)`;
  ctx.beginPath();
  for (let i = 0; i < n; i++) {
    if (i % 3 === 0) continue;
    place(i, gPt);
    const s = size * (0.55 + 0.7 * hash01(i, 99));
    ctx.moveTo(gPt[0] + s, gPt[1]);
    ctx.arc(gPt[0], gPt[1], s, 0, TAU);
  }
  ctx.fill();
  ctx.fillStyle = "rgba(255,255,255,0.55)";
  ctx.beginPath();
  for (let i = 1; i < n; i += 5) {
    place(i, gPt);
    const s = size * 0.35;
    ctx.moveTo(gPt[0] + s, gPt[1]);
    ctx.arc(gPt[0], gPt[1], s, 0, TAU);
  }
  ctx.fill();
}
function flashFill(ctx, fl) {
  ctx.fillStyle = `rgba(255,255,255,${0.8 * fl})`;
  ctx.fill();
}
class EnemyPainter {
  constructor(ctx) {
    this.ctx = ctx;
    for (const k of KINDS) this.kits.set(k, this.makeKit(k));
    this.whiteGlow = radial(ctx, 1, glowStops("255,255,255", 0.5));
  }
  ctx;
  kits = /* @__PURE__ */ new Map();
  whiteGlow;
  makeKit(k) {
    const { ctx } = this;
    const r = ENEMIES[k].radius;
    const col = P.enemy[k];
    const rgb = hexRgb(col);
    const deep = shadeRgb(col, 0.55);
    const light = tintRgb(col, 0.55);
    const shade = ctx.createRadialGradient(-0.4, -0.4, 0, -0.4, -0.4, 1.7);
    shade.addColorStop(0, `rgba(${light},0.5)`);
    shade.addColorStop(0.4, `rgba(${light},0)`);
    shade.addColorStop(0.72, "rgba(0,0,0,0.16)");
    shade.addColorStop(1, "rgba(0,0,0,0.45)");
    const orb = ctx.createRadialGradient(-0.35, -0.35, 0, 0, 0, 1.05);
    orb.addColorStop(0, `rgba(${light},0.75)`);
    orb.addColorStop(0.3, `rgba(${rgb},0.12)`);
    orb.addColorStop(0.72, `rgba(${deep},0.35)`);
    orb.addColorStop(1, `rgba(${deep},0.85)`);
    return {
      col,
      rgb,
      deep,
      light,
      bloom: radial(ctx, r * BLOOM_R[k], glowStops(rgb, k === "boss" ? 0.3 : 0.26)),
      shade,
      orb,
      fresnel: radial(ctx, 1, [
        [0, `rgba(${deep},0)`],
        [0.6, `rgba(${deep},0)`],
        [0.86, `rgba(${deep},0.28)`],
        [1, `rgba(${deep},0.65)`]
      ]),
      spec: radial(ctx, 1, [
        [0, "rgba(255,255,255,0.32)"],
        [0.5, "rgba(255,255,255,0.08)"],
        [1, "rgba(255,255,255,0)"]
      ]),
      cloud: radial(ctx, 1, glowStops(light, 0.2)),
      rodEdge: linear(ctx, 0, -ROD_W * r, 0, ROD_W * r, [
        [0, `rgba(${deep},0.55)`],
        [0.3, `rgba(${deep},0)`],
        [0.7, `rgba(${deep},0)`],
        [1, `rgba(${deep},0.55)`]
      ]),
      flag: linear(ctx, -ROD_L * r, 0, -(ROD_L + 3.2) * r, 0, [
        [0, `rgba(${light},0.85)`],
        [1, `rgba(${light},0.05)`]
      ]),
      streak: linear(ctx, -2 * r, 0, -3.6 * r, 0, [
        [0, `rgba(${rgb},0.25)`],
        [1, `rgba(${rgb},0)`]
      ]),
      sheath: linear(ctx, 0, -PH.sheath[2] * r, 0, PH.sheath[2] * r, [
        [0, `rgba(${deep},0.9)`],
        [0.3, `rgba(${rgb},0.6)`],
        [0.5, `rgba(${light},0.45)`],
        [0.7, `rgba(${rgb},0.6)`],
        [1, `rgba(${deep},0.9)`]
      ])
    };
  }
  draw(world) {
    const { ctx } = this;
    const time = world.time;
    let alive = 0;
    for (const e of world.enemies) if (e.alive) alive++;
    const detail = alive <= LOD_CROWD;
    for (const e of world.enemies) {
      if (!e.alive) continue;
      const kit = this.kits.get(e.kind);
      if (!kit) continue;
      const fl = Math.min(1, e.flash / 0.12);
      ctx.save();
      ctx.translate(e.x, e.y);
      const gr = e.radius * BLOOM_R[e.kind];
      ctx.fillStyle = kit.bloom;
      ctx.beginPath();
      ctx.arc(0, 0, gr, 0, TAU);
      ctx.fill();
      if (fl > 0) {
        ctx.save();
        ctx.globalAlpha = fl;
        ctx.scale(gr, gr);
        ctx.fillStyle = this.whiteGlow;
        ctx.beginPath();
        ctx.arc(0, 0, 1, 0, TAU);
        ctx.fill();
        ctx.restore();
      }
      const heading = Math.atan2(-e.y, -e.x);
      ctx.rotate(heading);
      ctx.lineCap = "round";
      ctx.lineJoin = "round";
      switch (e.kind) {
        case "basic":
          drawBasic(ctx, e, time, kit, heading, detail, fl);
          break;
        case "fast":
          drawFast(ctx, e, time, kit, heading, fl);
          break;
        case "tank":
          drawTank(ctx, e, time, kit, heading, detail, fl);
          break;
        case "ranged":
          drawRanged(ctx, e, time, kit, heading, fl);
          break;
        case "boss":
          drawBoss(ctx, e, time, kit, heading, detail, fl);
          break;
      }
      ctx.restore();
      if (fl > 0) {
        const k = Math.max(0, Math.min(1, 1 - e.flash / 0.12));
        if (k < 0.35) {
          ctx.lineWidth = 0.3;
          ctx.strokeStyle = `rgba(255,255,255,${(1 - k / 0.35) * 0.6})`;
          ctx.beginPath();
          ctx.arc(e.x, e.y, e.radius * (1 + k * 1.2), 0, TAU);
          ctx.stroke();
        }
      }
      if (e.dmgMult > 1.2) {
        const a = Math.min(0.6, (e.dmgMult - 1) * 0.6) * (0.8 + 0.2 * Math.sin(time * 6));
        ctx.fillStyle = `rgba(${P.dangerRgb},${a})`;
        ctx.beginPath();
        ctx.arc(e.x, e.y, e.radius * 0.55, 0, TAU);
        ctx.fill();
      }
      this.hpBar(e, kit);
    }
    ctx.lineCap = "butt";
    ctx.lineJoin = "miter";
  }
  /** Rounded glass bar above damaged, non-trivial enemies (boss: cilia extend the silhouette, so raise it). */
  hpBar(e, kit) {
    if (!(e.hp < e.maxHp && (e.kind === "boss" || e.kind === "tank" || e.hp / e.maxHp < 0.99))) return;
    const { ctx } = this;
    const boss = e.kind === "boss";
    const w = e.radius * (boss ? 3 : 2.4);
    const h = 0.55;
    const x = e.x - w / 2;
    const y = e.y - e.radius * (boss ? 1.5 : e.kind === "tank" ? 1.25 : 1) - 1.4;
    const frac = Math.max(0, e.hp / e.maxHp);
    const fill = boss ? P.danger : kit.col;
    ctx.fillStyle = "rgba(0,0,0,0.55)";
    ctx.beginPath();
    ctx.roundRect(x, y, w, h, h / 2);
    ctx.fill();
    ctx.lineWidth = 0.08;
    ctx.strokeStyle = `rgba(${kit.rgb},0.35)`;
    ctx.stroke();
    if (frac <= 0) return;
    ctx.fillStyle = rgba(fill, 0.25);
    ctx.beginPath();
    ctx.roundRect(x - 0.15, y - 0.15, w * frac + 0.3, h + 0.3, (h + 0.3) / 2);
    ctx.fill();
    ctx.fillStyle = fill;
    ctx.beginPath();
    ctx.roundRect(x, y, w * frac, h, h / 2);
    ctx.fill();
    ctx.fillStyle = "rgba(255,255,255,0.4)";
    ctx.fillRect(x + h / 2, y + 0.08, Math.max(0, w * frac - h), 0.12);
  }
}
function drawBasic(ctx, e, t, kit, rot, detail, fl) {
  const r = e.radius;
  const ph = e.id * 1.7;
  const L = ROD_L * r * (0.92 + 0.16 * hash01(e.id, 80));
  const W = ROD_W * r;
  const wig = 0.05 * Math.sin(t * 5 + ph);
  ctx.rotate(wig);
  rot += wig;
  flagellaTuftPath(ctx, r, L, t, ph, e.id);
  ctx.lineWidth = 0.3;
  ctx.strokeStyle = `rgba(${kit.deep},0.35)`;
  ctx.stroke();
  ctx.lineWidth = 0.11;
  ctx.strokeStyle = kit.flag;
  ctx.stroke();
  rodPath(ctx, L, W);
  ctx.fillStyle = `rgba(${kit.rgb},0.5)`;
  ctx.fill();
  ctx.fillStyle = kit.rodEdge;
  ctx.fill();
  lit(ctx, kit.shade, rot, 1.35 * r);
  local(ctx, kit.cloud, 0.62 * L, 0.55 * W, -0.05 * L, 0);
  wall(ctx, kit, 0.34);
  rodPath(ctx, L - 0.15 * r, W - 0.15 * r, 0.04);
  ctx.lineWidth = 0.07;
  ctx.strokeStyle = `rgba(${kit.light},0.28)`;
  ctx.stroke();
  if (hash01(e.id, 81) < 0.4) {
    const sx = 0.06 * L;
    ctx.beginPath();
    ctx.moveTo(sx, -0.9 * W);
    ctx.lineTo(sx, 0.9 * W);
    ctx.lineWidth = 0.12;
    ctx.strokeStyle = `rgba(${kit.deep},0.65)`;
    ctx.stroke();
  }
  foam(ctx, kit, detail ? 16 : 8, 0.09 * r, (i, out) => {
    const j = e.id * 16 + i;
    out[0] = (hash01(j, 71) - 0.5) * 2 * (L - W) + 0.05 * r * Math.sin(t * 0.6 + i * 1.9 + ph);
    out[1] = (hash01(j, 72) - 0.5) * 1.4 * W + 0.04 * r * Math.sin(t * 0.9 + i * 1.3);
  });
  toLocal(rot, LX, LY, lPt);
  const lx = lPt[0];
  const ly = lPt[1];
  const side = ly < 0 ? -1 : 1;
  ctx.beginPath();
  ctx.moveTo(-0.45 * L, side * 0.7 * W);
  ctx.lineTo(0.3 * L, side * 0.7 * W);
  ctx.lineWidth = 0.13;
  ctx.strokeStyle = "rgba(255,255,255,0.4)";
  ctx.stroke();
  ctx.fillStyle = "rgba(255,255,255,0.35)";
  ctx.beginPath();
  ctx.arc((lx > 0 ? L - W : W - L) + lx * 0.45 * W, ly * 0.45 * W, 0.1 * r, 0, TAU);
  ctx.fill();
  if (fl > 0) {
    rodPath(ctx, L, W);
    flashFill(ctx, fl);
  }
}
function drawFast(ctx, e, t, kit, rot, fl) {
  const r = e.radius;
  const ph = e.id * 2.3;
  const yaw = 0.05 * Math.sin(t * 9 + ph);
  ctx.rotate(yaw);
  rot += yaw;
  ctx.lineWidth = 0.5;
  ctx.strokeStyle = kit.streak;
  ctx.beginPath();
  ctx.moveTo(-2 * r, 0);
  ctx.lineTo(-3.6 * r, 0);
  ctx.stroke();
  spiroBodyPath(ctx, r, t, ph);
  ctx.fillStyle = `rgba(${kit.rgb},0.7)`;
  ctx.fill();
  lit(ctx, kit.shade, rot, 1.8 * r);
  ctx.lineWidth = 0.13;
  ctx.strokeStyle = `rgba(${kit.deep},0.85)`;
  ctx.stroke();
  spiroAxisPath(ctx, r, t, ph, 0, 0, 0.06, 0.94);
  ctx.lineWidth = 0.06;
  ctx.strokeStyle = `rgba(${kit.deep},0.5)`;
  ctx.stroke();
  toLocal(rot, LX, LY, lPt);
  const lx = lPt[0];
  const ly = lPt[1];
  spiroAxisPath(ctx, r, t, ph, lx * 0.09 * r, ly * 0.09 * r, 0.08, 0.92);
  ctx.lineWidth = 0.18;
  ctx.strokeStyle = `rgba(${kit.light},0.55)`;
  ctx.stroke();
  spiroAxisPath(ctx, r, t, ph, lx * 0.14 * r, ly * 0.14 * r, 0.15, 0.85);
  ctx.lineWidth = 0.06;
  ctx.strokeStyle = "rgba(255,255,255,0.5)";
  ctx.stroke();
  spiroGranulesPath(ctx, r, t, ph, 6, 0.06 * r);
  ctx.fillStyle = `rgba(${kit.light},0.7)`;
  ctx.fill();
  if (fl > 0) {
    spiroBodyPath(ctx, r, t, ph);
    flashFill(ctx, fl);
  }
}
function drawTank(ctx, e, t, kit, rot, detail, fl) {
  const r = e.radius;
  const ph = e.id * 1.3;
  const tumble = t * 0.2 + ph;
  ctx.rotate(tumble);
  rot += tumble;
  const pad = 0.32 + 0.025 * Math.sin(t * 1.4 + ph);
  cocciPath(ctx, r, pad);
  ctx.fillStyle = `rgba(${kit.rgb},0.09)`;
  ctx.fill();
  ctx.lineWidth = 0.14;
  ctx.strokeStyle = `rgba(${kit.light},0.16)`;
  ctx.stroke();
  cocciPath(ctx, r, pad * 0.5);
  ctx.fillStyle = `rgba(${kit.rgb},0.08)`;
  ctx.fill();
  ctx.fillStyle = `rgba(${kit.light},0.22)`;
  ctx.beginPath();
  for (let i = 0; i < 6; i++) {
    const a = hash01(e.id * 16 + i, 83) * TAU + t * 0.05;
    const d = (0.98 + 0.18 * hash01(e.id * 16 + i, 84)) * r;
    ctx.moveTo(Math.cos(a) * d + 0.05 * r, Math.sin(a) * d);
    ctx.arc(Math.cos(a) * d, Math.sin(a) * d, 0.05 * r, 0, TAU);
  }
  ctx.fill();
  cocciPath(ctx, r, 0.04);
  ctx.fillStyle = `rgba(${kit.deep},0.7)`;
  ctx.fill();
  for (const c of COCCI) {
    const cr = c.s * r;
    const cx = c.x * r;
    const cy = c.y * r;
    ctx.beginPath();
    ctx.arc(cx, cy, cr, 0, TAU);
    ctx.fillStyle = `rgba(${kit.rgb},0.62)`;
    ctx.fill();
    lit(ctx, kit.orb, rot, cr, cx, cy);
    ctx.lineWidth = 0.1;
    ctx.strokeStyle = `rgba(${kit.deep},0.8)`;
    ctx.stroke();
  }
  ctx.lineWidth = 0.08;
  ctx.strokeStyle = `rgba(${kit.deep},0.7)`;
  ctx.beginPath();
  for (const idx of COCCI_DIVIDING) {
    const c = COCCI[idx];
    const a = hash01(e.id, 85 + idx) * Math.PI;
    const dx = Math.cos(a) * 0.92 * c.s * r;
    const dy = Math.sin(a) * 0.92 * c.s * r;
    ctx.moveTo(c.x * r - dx, c.y * r - dy);
    ctx.lineTo(c.x * r + dx, c.y * r + dy);
  }
  ctx.stroke();
  foam(ctx, kit, detail ? COCCI.length * 2 : COCCI.length, 0.07 * r, (i, out) => {
    const c = COCCI[i % COCCI.length];
    const j = e.id * 16 + i;
    const a = hash01(j, 73) * TAU + t * 0.3 * (i % 2 ? 1 : -1);
    const d = (0.2 + 0.35 * hash01(j, 74)) * c.s * r;
    out[0] = c.x * r + Math.cos(a) * d;
    out[1] = c.y * r + Math.sin(a) * d;
  });
  toLocal(rot, LX, LY, lPt);
  ctx.fillStyle = "rgba(255,255,255,0.5)";
  ctx.beginPath();
  for (const c of COCCI) {
    const cr = c.s * r;
    const hx = c.x * r + lPt[0] * 0.4 * cr;
    const hy = c.y * r + lPt[1] * 0.4 * cr;
    ctx.moveTo(hx + 0.1 * cr, hy);
    ctx.arc(hx, hy, 0.1 * cr, 0, TAU);
  }
  ctx.fill();
  if (fl > 0) {
    cocciPath(ctx, r, 0);
    flashFill(ctx, fl);
  }
}
function drawRanged(ctx, e, t, kit, rot, fl) {
  const r = e.radius;
  const ph = e.id * 3.1;
  const rec = Math.max(0, (e.attackCd - (e.def.attackInterval - 0.25)) / 0.25);
  const charge = e.arrived && e.attackCd < 0.5 ? 1 - e.attackCd / 0.5 : 0;
  const spread = 1 - 0.35 * rec;
  ctx.translate(-0.3 * r * rec, 0);
  phageFibersPath(ctx, r, t, ph, spread);
  ctx.lineWidth = 0.24;
  ctx.strokeStyle = `rgba(${kit.deep},0.5)`;
  ctx.stroke();
  ctx.lineWidth = 0.08;
  ctx.strokeStyle = `rgba(${kit.light},0.75)`;
  ctx.stroke();
  phageFiberTipsPath(ctx, r, t, ph, spread, 0.07 * r);
  ctx.fillStyle = "rgba(255,255,255,0.6)";
  ctx.fill();
  phagePinsPath(ctx, r);
  ctx.lineWidth = 0.07;
  ctx.strokeStyle = `rgba(${kit.light},0.6)`;
  ctx.stroke();
  phageWhiskersPath(ctx, r);
  ctx.lineWidth = 0.06;
  ctx.strokeStyle = `rgba(${kit.light},0.5)`;
  ctx.stroke();
  phageSheathPath(ctx, r);
  ctx.fillStyle = kit.sheath;
  ctx.fill();
  ctx.lineWidth = 0.06;
  ctx.strokeStyle = `rgba(${kit.deep},0.8)`;
  ctx.stroke();
  phageStriationsPath(ctx, r);
  ctx.lineWidth = 0.05;
  ctx.strokeStyle = `rgba(${kit.deep},0.55)`;
  ctx.stroke();
  phageCollarPath(ctx, r);
  ctx.fillStyle = `rgba(${kit.light},0.55)`;
  ctx.fill();
  ctx.lineWidth = 0.06;
  ctx.strokeStyle = `rgba(${kit.deep},0.8)`;
  ctx.stroke();
  phagePlatePath(ctx, r);
  ctx.fillStyle = `rgba(${kit.deep},0.9)`;
  ctx.fill();
  ctx.lineWidth = 0.07;
  ctx.strokeStyle = `rgba(${kit.light},0.7)`;
  ctx.stroke();
  if (charge > 0) {
    phageSheathPath(ctx, r);
    ctx.fillStyle = `rgba(${kit.light},${0.55 * charge})`;
    ctx.fill();
    ctx.fillStyle = `rgba(255,255,255,${0.5 * charge * charge})`;
    ctx.beginPath();
    ctx.arc(PH.plate[1] * r, 0, 0.2 * r, 0, TAU);
    ctx.fill();
  }
  phageHeadPath(ctx, r);
  ctx.fillStyle = `rgba(${kit.rgb},0.6)`;
  ctx.fill();
  local(ctx, kit.fresnel, PH.rx * 1.05 * r, PH.ry * 1.05 * r, PH.hx * r, 0);
  lit(ctx, kit.shade, rot, 0.9 * r, PH.hx * r, 0);
  phageFacetsPath(ctx, r, 0);
  ctx.fillStyle = `rgba(${kit.light},0.16)`;
  ctx.fill();
  phageFacetsPath(ctx, r, 2);
  ctx.fillStyle = "rgba(0,0,0,0.2)";
  ctx.fill();
  phageEdgesPath(ctx, r);
  ctx.lineWidth = 0.06;
  ctx.strokeStyle = `rgba(${kit.light},0.45)`;
  ctx.stroke();
  phageHeadPath(ctx, r);
  wall(ctx, kit, 0.3);
  const grot = t * 0.15 + ph;
  phageGenomePath(ctx, r, grot);
  ctx.lineWidth = 0.06;
  ctx.strokeStyle = `rgba(${kit.light},0.35)`;
  ctx.stroke();
  phageGenomeDotsPath(ctx, r, grot, 0.055 * r);
  ctx.fillStyle = `rgba(${kit.light},0.7)`;
  ctx.fill();
  toLocal(rot, LX, LY, lPt);
  ctx.fillStyle = "rgba(255,255,255,0.4)";
  ctx.beginPath();
  ctx.arc(PH.hx * r + lPt[0] * 0.42 * r, lPt[1] * 0.34 * r, 0.09 * r, 0, TAU);
  ctx.fill();
  if (rec > 0.6) {
    ctx.fillStyle = `rgba(255,255,255,${(rec - 0.6) * 2})`;
    ctx.beginPath();
    ctx.arc(PH.plate[1] * r + 0.1 * r, 0, 0.25 * r, 0, TAU);
    ctx.fill();
  }
  if (fl > 0) {
    phageSilhouettePath(ctx, r);
    flashFill(ctx, fl);
  }
}
function drawBoss(ctx, e, t, kit, rot, detail, fl) {
  const r = e.radius;
  const ph = e.id * 0.9;
  const sway = 0.03 * Math.sin(t * 1.4 + ph);
  ctx.rotate(sway);
  rot += sway;
  ciliaPath(ctx, r, t, ph, detail ? 96 : 64);
  ctx.lineWidth = 0.18;
  ctx.strokeStyle = `rgba(${kit.deep},0.45)`;
  ctx.stroke();
  ctx.lineWidth = 0.07;
  ctx.strokeStyle = `rgba(${kit.light},0.7)`;
  ctx.stroke();
  ciliateBodyPath(ctx, r, t, ph);
  ctx.fillStyle = `rgba(${kit.rgb},0.5)`;
  ctx.fill();
  local(ctx, kit.fresnel, CILIATE_RX * 1.03 * r, CILIATE_RY * 1.05 * r);
  lit(ctx, kit.shade, rot, 1.45 * r);
  toLocal(rot, -0.5 * r, -0.3 * r, lPt);
  lit(ctx, kit.spec, rot, 0.75 * r, lPt[0], lPt[1]);
  ciliateRindPath(ctx, r, t, ph, detail ? 84 : 42);
  ctx.lineWidth = 0.05;
  ctx.strokeStyle = `rgba(${kit.light},0.24)`;
  ctx.stroke();
  ciliateBodyPath(ctx, r, t, ph);
  wall(ctx, kit, 0.5);
  ctx.save();
  ctx.scale(0.93, 0.93);
  ciliateBodyPath(ctx, r, t, ph);
  ctx.restore();
  ctx.lineWidth = 0.08;
  ctx.strokeStyle = `rgba(${kit.light},0.2)`;
  ctx.stroke();
  ciliateGrooveWedgePath(ctx, r, t, ph);
  ctx.fillStyle = "rgba(0,0,0,0.3)";
  ctx.fill();
  ciliateGrooveLinePath(ctx, r, t, ph);
  ctx.lineWidth = 0.12;
  ctx.strokeStyle = `rgba(${kit.light},0.5)`;
  ctx.stroke();
  ciliateCytostome(gPt, r, t, ph);
  ctx.fillStyle = `rgba(${kit.deep},0.9)`;
  ctx.beginPath();
  ctx.arc(gPt[0], gPt[1], 0.11 * r, 0, TAU);
  ctx.fill();
  ctx.lineWidth = 0.06;
  ctx.strokeStyle = `rgba(${kit.light},0.6)`;
  ctx.stroke();
  foam(ctx, kit, detail ? 27 : 12, 0.06 * r, (i, out) => {
    const j = e.id * 16 + i;
    const a = hash01(j, 76) * TAU + t * 0.1;
    const d = 0.3 + 0.6 * hash01(j, 77);
    out[0] = Math.cos(a) * d * 1.12 * r;
    out[1] = Math.sin(a) * d * 0.6 * r;
  });
  for (let i = 0; i < 5; i++) {
    const a = hash01(i, 21) * TAU + t * 0.06;
    const d = 0.4 + 0.45 * hash01(i, 22);
    const vx = Math.cos(a) * d * 1.05 * r;
    const vy = Math.sin(a) * d * 0.55 * r;
    const vr = (0.09 + 0.06 * hash01(i, 23)) * r;
    ctx.beginPath();
    ctx.arc(vx, vy, vr, 0, TAU);
    ctx.fillStyle = `rgba(${kit.deep},0.55)`;
    ctx.fill();
    lit(ctx, kit.orb, rot, vr, vx, vy);
    ctx.lineWidth = 0.06;
    ctx.strokeStyle = `rgba(${kit.light},0.45)`;
    ctx.stroke();
    ctx.fillStyle = "rgba(0,0,0,0.35)";
    ctx.beginPath();
    ctx.arc(vx + 0.15 * vr, vy + 0.1 * vr, 0.35 * vr, 0, TAU);
    ctx.fill();
  }
  for (let i = 0; i < 2; i++) {
    const vx = (i === 0 ? 0.7 : -0.72) * r;
    const vy = 0.3 * r;
    const k = Math.abs(Math.sin(t * 0.8 + i * Math.PI));
    const vr = (0.07 + 0.09 * k) * r;
    const len = (0.32 - 0.1 * k) * r;
    const crot = i * 0.4;
    canalsPath(ctx, vx, vy, 7, vr, len, crot);
    ctx.lineWidth = 0.08;
    ctx.strokeStyle = `rgba(${kit.light},0.45)`;
    ctx.stroke();
    canalBulbsPath(ctx, vx, vy, 7, len, crot, 0.035 * r);
    ctx.fillStyle = `rgba(${kit.light},0.55)`;
    ctx.fill();
    ctx.beginPath();
    ctx.arc(vx, vy, vr, 0, TAU);
    ctx.fillStyle = `rgba(${kit.light},0.22)`;
    ctx.fill();
    ctx.lineWidth = 0.1;
    ctx.strokeStyle = `rgba(${kit.light},0.65)`;
    ctx.stroke();
  }
  ctx.save();
  ctx.translate(-0.12 * r, -0.04 * r);
  ctx.rotate(0.25);
  const ns = 0.46 * r;
  beanPath(ctx, ns);
  ctx.fillStyle = `rgba(${kit.deep},0.72)`;
  ctx.fill();
  lit(ctx, kit.shade, rot + 0.25, 0.55 * r);
  ctx.lineWidth = 0.16;
  ctx.strokeStyle = `rgba(${kit.deep},0.9)`;
  ctx.stroke();
  ctx.lineWidth = 0.07;
  ctx.strokeStyle = `rgba(${kit.light},0.6)`;
  ctx.stroke();
  ctx.fillStyle = `rgba(${kit.light},0.5)`;
  ctx.beginPath();
  for (let i = 0; i < 7; i++) {
    const cx = (hash01(i, 31) - 0.5) * 1.4 * ns;
    const cy = (hash01(i, 32) - 0.35) * 0.9 * ns;
    ctx.moveTo(cx + 0.05 * r, cy);
    ctx.arc(cx, cy, 0.05 * r, 0, TAU);
  }
  ctx.fill();
  ctx.restore();
  ctx.beginPath();
  ctx.arc(0.38 * r, -0.3 * r, 0.09 * r, 0, TAU);
  ctx.fillStyle = `rgba(${kit.light},0.85)`;
  ctx.fill();
  ctx.lineWidth = 0.06;
  ctx.strokeStyle = `rgba(${kit.deep},0.8)`;
  ctx.stroke();
  if (fl > 0) {
    ciliateBodyPath(ctx, r, t, ph);
    flashFill(ctx, fl);
  }
}
export {
  EnemyPainter
};
