import { CELL_RADIUS } from "./constants.js";
import { TAU, bacillusPath, blobPath, hash01, makeGranules, makeOrganelles, mitoCristaePath } from "./bioShapes.js";
import { P, glowStops, radial } from "./palette.js";
const VESICLES = 26;
class CellPainter {
  constructor(ctx) {
    this.ctx = ctx;
    const r = CELL_RADIUS;
    this.halo = radial(ctx, r * 3, glowStops(P.cyanRgb, 0.25), 0, 0, r * 0.4);
    this.haloHurt = radial(ctx, r * 3, glowStops(P.dangerRgb, 0.4), 0, 0, r * 0.4);
    this.glass = radial(ctx, r * 1.35, [
      [0, "rgba(14,52,66,0.82)"],
      [0.72, "rgba(26,96,106,0.72)"],
      [1, `rgba(${P.cyanRgb},0.38)`]
    ]);
    this.nucGlow = radial(ctx, r * 0.9, glowStops(P.pinkRgb, 0.45));
    this.nucleus = radial(ctx, r * 0.4, [
      [0, "#ff9ad2"],
      [0.55, "#b04b9a"],
      [1, "rgba(120,50,150,0.9)"]
    ], -r * 0.08, -r * 0.08);
  }
  ctx;
  buf = new Float64Array(2 * 20);
  granules = makeGranules(6);
  organelles = makeOrganelles(4);
  halo;
  haloHurt;
  glass;
  nucGlow;
  nucleus;
  /** Chemotaxis aura: soft band, dashed ring at the true range, outgoing sonar pulse. */
  drawRange(world) {
    const { ctx } = this;
    const r = world.stats.range;
    const time = world.time;
    const pulse = 0.5 + 0.5 * Math.sin(time * 1.4);
    const rr = r * (1 + 0.02 * pulse);
    ctx.fillStyle = radial(ctx, rr, [
      [0, `rgba(${P.cyanRgb},0)`],
      [0.72, `rgba(${P.cyanRgb},${0.015 + 0.01 * pulse})`],
      [0.94, `rgba(${P.cyanRgb},${0.05 + 0.025 * pulse})`],
      [1, `rgba(${P.cyanRgb},0)`]
    ], 0, 0, r * 0.3);
    ctx.beginPath();
    ctx.arc(0, 0, rr, 0, TAU);
    ctx.fill();
    ctx.setLineDash([1.2, 2.4]);
    ctx.lineDashOffset = -time * 1.5;
    ctx.lineWidth = 0.3;
    ctx.strokeStyle = `rgba(${P.cyanRgb},0.22)`;
    ctx.beginPath();
    ctx.arc(0, 0, r, 0, TAU);
    ctx.stroke();
    ctx.setLineDash([]);
    ctx.lineDashOffset = 0;
    const k = (time * 0.45 % 1 + 1) % 1;
    ctx.lineWidth = 0.5;
    ctx.strokeStyle = `rgba(${P.cyanRgb},${(1 - k) * 0.18})`;
    ctx.beginPath();
    ctx.arc(0, 0, r * k, 0, TAU);
    ctx.stroke();
  }
  draw(world) {
    const { ctx } = this;
    const r = CELL_RADIUS;
    const t = world.cell;
    const time = world.time;
    const frac = Math.max(0, t.hp / t.maxHp);
    const hurt = t.flash > 0;
    const low = frac < 0.3;
    const fired = Math.max(0, Math.min(1, t.fireCd * world.stats.attackSpeed));
    const rm = r * 1.15 * (1 - 0.04 * fired);
    const blink = 0.5 + 0.5 * Math.sin(time * 6);
    const danger = hurt || low && blink > 0.5;
    const rim = danger ? P.dangerRgb : P.cyanRgb;
    ctx.fillStyle = hurt ? this.haloHurt : this.halo;
    ctx.beginPath();
    ctx.arc(0, 0, r * 3, 0, TAU);
    ctx.fill();
    blobPath(ctx, this.buf, rm, time, 0.05, 0.45, 0);
    ctx.fillStyle = this.glass;
    ctx.fill();
    if (hurt) {
      ctx.fillStyle = `rgba(${P.dangerRgb},0.35)`;
      ctx.fill();
    }
    ctx.lineJoin = "round";
    ctx.lineWidth = 2.6;
    ctx.strokeStyle = `rgba(${rim},0.08)`;
    ctx.stroke();
    ctx.lineWidth = 1.4;
    ctx.strokeStyle = `rgba(${rim},0.18)`;
    ctx.stroke();
    ctx.lineWidth = 0.45;
    ctx.strokeStyle = `rgba(${rim},0.95)`;
    ctx.stroke();
    ctx.fillStyle = `rgba(${P.cyanRgb},0.18)`;
    ctx.beginPath();
    for (let i = 0; i < VESICLES; i++) {
      const a = i / VESICLES * TAU + time * 0.05;
      const rr = 0.9 * rm * (1 + 0.02 * Math.sin(time * 1.3 + i * 1.7));
      const s = r * 0.07 * (0.8 + 0.4 * hash01(i, 51));
      const x = Math.cos(a) * rr;
      const y = Math.sin(a) * rr;
      ctx.moveTo(x + s, y);
      ctx.arc(x, y, s, 0, TAU);
    }
    ctx.fill();
    ctx.setLineDash([0.6, 0.9]);
    ctx.lineDashOffset = -time * 0.8;
    ctx.lineWidth = 0.12;
    ctx.strokeStyle = `rgba(${P.cyanRgb},0.14)`;
    ctx.beginPath();
    ctx.arc(0, 0, r * 0.82, 0, TAU);
    ctx.stroke();
    ctx.setLineDash([]);
    ctx.lineDashOffset = 0;
    for (const o of this.organelles) {
      const a = o.ang + time * o.speed;
      const rr = r * o.rad * (1 + 0.04 * Math.sin(time * 0.6 + o.phase));
      ctx.save();
      ctx.translate(Math.cos(a) * rr, Math.sin(a) * rr);
      ctx.rotate(o.tilt + time * o.tiltSpeed);
      const br = r * o.size / 1.25;
      bacillusPath(ctx, br);
      ctx.fillStyle = "rgba(120,90,220,0.55)";
      ctx.fill();
      ctx.lineWidth = 0.06;
      ctx.strokeStyle = `rgba(${P.violetRgb},0.6)`;
      ctx.stroke();
      mitoCristaePath(ctx, 1.25 * br, 0.62 * br);
      ctx.lineWidth = 0.05;
      ctx.strokeStyle = "rgba(255,255,255,0.3)";
      ctx.stroke();
      ctx.restore();
    }
    for (const g of this.granules) {
      const a = g.ang + time * g.speed;
      const rr = r * g.rad * (1 + 0.06 * Math.sin(time * 0.7 + g.phase));
      const x = Math.cos(a) * rr;
      const y = Math.sin(a) * rr;
      const s = r * g.size * 0.6;
      ctx.fillStyle = "rgba(200,255,250,0.35)";
      ctx.beginPath();
      ctx.arc(x, y, s, 0, TAU);
      ctx.fill();
      ctx.fillStyle = "rgba(255,255,255,0.5)";
      ctx.beginPath();
      ctx.arc(x - s * 0.3, y - s * 0.3, s * 0.3, 0, TAU);
      ctx.fill();
    }
    const nx = Math.cos(time * 0.13) * r * 0.08;
    const ny = Math.sin(time * 0.11) * r * 0.08;
    ctx.save();
    ctx.translate(nx, ny);
    ctx.lineWidth = 0.12;
    ctx.strokeStyle = `rgba(${P.violetRgb},0.25)`;
    for (let i = 0; i < 3; i++) {
      const rad = (0.5 + 0.08 * i) * r;
      const start = time * 0.1 + i * 0.9 + (i % 2 ? Math.PI : 0);
      ctx.beginPath();
      ctx.arc(0, 0, rad, start, start + 0.9 * Math.PI);
      ctx.stroke();
    }
    ctx.globalAlpha = 0.8 + 0.2 * Math.sin(time * 2);
    ctx.fillStyle = this.nucGlow;
    ctx.beginPath();
    ctx.arc(0, 0, r * 0.9, 0, TAU);
    ctx.fill();
    ctx.globalAlpha = 1;
    ctx.fillStyle = this.nucleus;
    ctx.beginPath();
    ctx.arc(0, 0, r * 0.36, 0, TAU);
    ctx.fill();
    ctx.lineWidth = 0.12;
    ctx.strokeStyle = `rgba(${P.pinkRgb},0.8)`;
    ctx.stroke();
    ctx.fillStyle = "rgba(255,255,255,0.5)";
    ctx.beginPath();
    for (let i = 0; i < 3; i++) {
      const a = hash01(i, 61) * TAU + time * 0.05;
      const d = r * (0.15 + hash01(i, 62) * 0.08);
      const s = r * 0.045;
      const x = Math.cos(a) * d;
      const y = Math.sin(a) * d;
      ctx.moveTo(x + s, y);
      ctx.arc(x, y, s, 0, TAU);
    }
    ctx.fill();
    if (fired > 0.7) {
      ctx.fillStyle = `rgba(255,255,255,${(fired - 0.7) * 1.2})`;
      ctx.beginPath();
      ctx.arc(0, 0, r * 0.36, 0, TAU);
      ctx.fill();
    }
    ctx.restore();
    ctx.lineCap = "round";
    ctx.lineWidth = 0.28;
    ctx.strokeStyle = `rgba(${P.cyanRgb},0.1)`;
    ctx.beginPath();
    ctx.arc(0, 0, r * 1.85, 0, TAU);
    ctx.stroke();
    const hp = low ? P.dangerRgb : P.pinkRgb;
    ctx.beginPath();
    ctx.arc(0, 0, r * 1.85, -Math.PI / 2, -Math.PI / 2 + TAU * frac);
    ctx.lineWidth = 0.7;
    ctx.strokeStyle = `rgba(${hp},0.15)`;
    ctx.stroke();
    ctx.lineWidth = 0.25;
    ctx.strokeStyle = `rgba(${hp},0.85)`;
    ctx.stroke();
    ctx.lineCap = "butt";
  }
}
export {
  CellPainter
};
