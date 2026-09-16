import { TAU, hash01, helixNodesPath, helixPath, helixRungsPath, makeParticles } from "./bioShapes.js";
import { P, radial } from "./palette.js";
import { LOW_INTEGRITY } from "./constants.js";
function makeBokeh(n) {
  const out = [];
  for (let i = 0; i < n; i++) {
    const rgb = i < 6 ? P.cyanRgb : i < 9 ? P.violetRgb : P.magentaRgb;
    out.push({
      fx: 0.05 + hash01(i, 31) * 0.9,
      fy: 0.05 + hash01(i, 32) * 0.9,
      r: 0.08 + hash01(i, 33) * 0.14,
      rgb,
      a: 0.05 + hash01(i, 34) * 0.07
    });
  }
  return out;
}
const TICKS = 72;
class Backdrop {
  cache = null;
  bokeh = makeBokeh(10);
  motes = makeParticles(60);
  /** Rebuild the static layers for the given CSS size, device pixel ratio, dish radius (px) and dish centre. */
  rebuild(w, h, dpr, R, cx = w / 2, cy = h / 2) {
    const c = this.cache ?? document.createElement("canvas");
    c.width = Math.max(1, Math.floor(w * dpr));
    c.height = Math.max(1, Math.floor(h * dpr));
    this.cache = c;
    const ctx = c.getContext("2d");
    if (!ctx) return;
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    const big = Math.max(w, h) * 0.75;
    const m = Math.min(w, h);
    ctx.fillStyle = P.bg;
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = radial(ctx, big, [
      [0, P.fieldCenter],
      [0.55, P.fieldMid],
      [1, P.fieldEdge]
    ], cx, cy);
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = radial(ctx, m * 0.8, [
      [0, `rgba(${P.cyanRgb},0.05)`],
      [1, `rgba(${P.cyanRgb},0)`]
    ], w * 0.2, h * 0.15);
    ctx.fillRect(0, 0, w, h);
    for (const b of this.bokeh) {
      const x = b.fx * w;
      const y = b.fy * h;
      const r = b.r * m;
      ctx.fillStyle = radial(ctx, r, [
        [0, `rgba(${b.rgb},${b.a * 0.35})`],
        [0.6, `rgba(${b.rgb},${b.a * 0.5})`],
        [0.78, `rgba(${b.rgb},${b.a})`],
        [1, `rgba(${b.rgb},0)`]
      ], x, y);
      ctx.beginPath();
      ctx.arc(x, y, r, 0, TAU);
      ctx.fill();
    }
    if (R < big * 1.6) {
      ctx.lineCap = "butt";
      const passes = [
        [6, 0.04],
        [3, 0.1],
        [1.25, 0.28]
      ];
      for (const [lw, a] of passes) {
        ctx.lineWidth = lw;
        ctx.strokeStyle = `rgba(${P.cyanRgb},${a})`;
        ctx.beginPath();
        ctx.arc(cx, cy, R, 0, TAU);
        ctx.stroke();
      }
      ctx.lineWidth = 1;
      ctx.strokeStyle = `rgba(${P.cyanRgb},0.18)`;
      ctx.beginPath();
      for (let i = 0; i < TICKS; i++) {
        const a = i / TICKS * TAU;
        const len = i % 6 === 0 ? 8 : 4;
        const ca = Math.cos(a);
        const sa = Math.sin(a);
        ctx.moveTo(cx + ca * (R + 3), cy + sa * (R + 3));
        ctx.lineTo(cx + ca * (R + 3 + len), cy + sa * (R + 3 + len));
      }
      ctx.stroke();
      ctx.strokeStyle = `rgba(${P.cyanRgb},0.06)`;
      ctx.beginPath();
      ctx.arc(cx, cy, R * 0.97, 0, TAU);
      ctx.stroke();
    }
    ctx.fillStyle = radial(ctx, big, [
      [0, "rgba(0,0,0,0)"],
      [0.35, "rgba(0,0,0,0.35)"],
      [1, "rgba(0,0,0,0.8)"]
    ], cx, cy, Math.min(R * 0.85, big * 0.8));
    ctx.fillRect(0, 0, w, h);
  }
  /** Cached layers + live layer. Expects the identity (dpr) transform. */
  draw(ctx, world, w, h, cx = w / 2, cy = h / 2) {
    if (this.cache) ctx.drawImage(this.cache, 0, 0, w, h);
    else {
      ctx.fillStyle = P.bg;
      ctx.fillRect(0, 0, w, h);
    }
    const time = world.time;
    const big = Math.max(w, h) * 0.75;
    const sx = cx + Math.cos(time * 0.11) * w * 0.3;
    const sy = cy + Math.sin(time * 0.07) * h * 0.3;
    ctx.fillStyle = radial(ctx, big * 0.7, [
      [0, `rgba(${P.cyanRgb},0.04)`],
      [1, `rgba(${P.cyanRgb},0)`]
    ], sx, sy);
    ctx.fillRect(0, 0, w, h);
    this.drawHelices(ctx, w, h, time);
    this.drawMotes(ctx, cx, cy, big, time);
    const hpFrac = world.cell.hp / world.cell.maxHp;
    if (hpFrac < LOW_INTEGRITY) {
      const pulse = 0.5 + 0.5 * Math.sin(time * 8);
      ctx.fillStyle = `rgba(${P.dangerRgb},${0.08 * pulse * (1 - hpFrac / LOW_INTEGRITY)})`;
      ctx.fillRect(0, 0, w, h);
    }
  }
  drawHelices(ctx, w, h, time) {
    const A = Math.min(20, Math.max(10, h * 0.05));
    const L = h * 0.75;
    const segs = 48;
    const turns = L / (A * 4.5);
    const anchors = [
      [-0.04 * w, 0.55 * h, -1.22, time * 0.3],
      [1.03 * w, 0.32 * h, 2.12, -time * 0.24 + 1.7]
    ];
    ctx.lineCap = "round";
    for (const [x, y, rot, phase] of anchors) {
      ctx.save();
      ctx.translate(x, y);
      ctx.rotate(rot);
      ctx.lineWidth = 1.2;
      ctx.strokeStyle = `rgba(${P.cyanRgb},0.1)`;
      helixPath(ctx, L, A, segs, turns, phase, 0);
      ctx.stroke();
      ctx.strokeStyle = `rgba(${P.cyanRgb},0.07)`;
      helixPath(ctx, L, A, segs, turns, phase, 1);
      ctx.stroke();
      ctx.lineWidth = 1;
      ctx.strokeStyle = `rgba(${P.violetRgb},0.08)`;
      helixRungsPath(ctx, L, A, segs, turns, phase, 3);
      ctx.stroke();
      ctx.fillStyle = `rgba(${P.cyanRgb},0.18)`;
      helixNodesPath(ctx, L, A, segs, turns, phase, 6, 1.8);
      ctx.fill();
      ctx.restore();
    }
  }
  /** Suspended motes: cyan (a few violet) dots with a soft halo, bubbles as faint rings. */
  drawMotes(ctx, cx, cy, big, time) {
    const pos = (p) => {
      const a = p.ang + time * p.spin;
      const rr = p.rad * big + Math.sin(time * p.bob + p.phase) * big * 8e-3;
      return [cx + Math.cos(a) * rr, cy + Math.sin(a) * rr];
    };
    ctx.fillStyle = `rgba(${P.cyanRgb},0.08)`;
    ctx.beginPath();
    for (const p of this.motes) {
      if (p.bubble) continue;
      const [x, y] = pos(p);
      const s = (0.8 + p.size * 3) * 2.2;
      ctx.moveTo(x + s, y);
      ctx.arc(x, y, s, 0, TAU);
    }
    ctx.fill();
    for (const violet of [false, true]) {
      ctx.fillStyle = violet ? `rgba(${P.violetRgb},0.4)` : `rgba(${P.cyanRgb},0.35)`;
      ctx.beginPath();
      let i = 0;
      for (const p of this.motes) {
        const isViolet = i++ % 7 === 0;
        if (p.bubble || isViolet !== violet) continue;
        const [x, y] = pos(p);
        const s = 0.8 + p.size * 3;
        ctx.moveTo(x + s, y);
        ctx.arc(x, y, s, 0, TAU);
      }
      ctx.fill();
    }
    ctx.lineWidth = 1;
    ctx.strokeStyle = `rgba(${P.cyanRgb},0.12)`;
    ctx.beginPath();
    for (const p of this.motes) {
      if (!p.bubble) continue;
      const [x, y] = pos(p);
      const s = (0.8 + p.size * 3) * 2.6;
      ctx.moveTo(x + s, y);
      ctx.arc(x, y, s, 0, TAU);
    }
    ctx.stroke();
  }
}
export {
  Backdrop
};
