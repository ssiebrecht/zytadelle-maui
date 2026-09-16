import { TAU, hash01 } from "./bioShapes.js";
import { FADE, FX_LIFETIME } from "./constants.js";
import { FONT, P, glowStops, hexRgb, radial } from "./palette.js";
const HEAD_RGB = { cyan: P.cyanRgb, amber: P.amberRgb, enemy: hexRgb(P.enemy.ranged) };
class FxPainter {
  constructor(ctx) {
    this.ctx = ctx;
    this.heads = {
      cyan: radial(ctx, 1, glowStops(P.cyanRgb, 0.55)),
      amber: radial(ctx, 1, glowStops(P.amberRgb, 0.6)),
      enemy: radial(ctx, 1, glowStops(HEAD_RGB.enemy, 0.55))
    };
  }
  ctx;
  heads;
  drawProjectiles(world) {
    const { ctx } = this;
    ctx.lineCap = "round";
    for (const p of world.projectiles) {
      if (!p.alive) continue;
      const key = p.fromCell ? p.crit ? "amber" : "cyan" : "enemy";
      const rgb = HEAD_RGB[key];
      const len = p.fromCell ? p.crit ? 3.2 : 2.4 : 1.6;
      const d = Math.hypot(p.vx, p.vy) || 1;
      const tx = p.x - p.vx / d * len;
      const ty = p.y - p.vy / d * len;
      ctx.beginPath();
      ctx.moveTo(p.x, p.y);
      ctx.lineTo(tx, ty);
      ctx.lineWidth = p.crit ? 1.6 : 1.1;
      ctx.strokeStyle = `rgba(${rgb},0.18)`;
      ctx.stroke();
      ctx.lineWidth = p.crit ? 0.6 : 0.4;
      ctx.strokeStyle = `rgba(${rgb},0.9)`;
      ctx.stroke();
      const gr = p.crit ? 1.6 : 1.1;
      ctx.save();
      ctx.translate(p.x, p.y);
      ctx.scale(gr, gr);
      ctx.fillStyle = this.heads[key];
      ctx.beginPath();
      ctx.arc(0, 0, 1, 0, TAU);
      ctx.fill();
      ctx.restore();
      ctx.fillStyle = P.white;
      ctx.beginPath();
      ctx.arc(p.x, p.y, p.crit ? 0.5 : 0.35, 0, TAU);
      ctx.fill();
      if (p.crit) {
        const a0 = world.time * 6;
        ctx.lineWidth = 0.2;
        ctx.strokeStyle = `rgba(${P.amberRgb},0.7)`;
        ctx.beginPath();
        for (let i = 0; i < 2; i++) {
          const a = a0 + i * Math.PI / 2;
          const c = Math.cos(a) * 1.2;
          const s = Math.sin(a) * 1.2;
          ctx.moveTo(p.x + c, p.y + s);
          ctx.lineTo(p.x - c, p.y - s);
        }
        ctx.stroke();
      }
    }
    ctx.lineCap = "butt";
  }
  drawFx(world) {
    const { ctx } = this;
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    ctx.lineJoin = "round";
    for (const f of world.fx) {
      const a = Math.max(0, 1 - f.t / FX_LIFETIME);
      switch (f.kind) {
        case "kill": {
          ctx.strokeStyle = `rgba(200,255,250,${a * 0.8})`;
          ctx.lineWidth = 0.4;
          ctx.beginPath();
          ctx.arc(f.x, f.y, 1 + f.t * 5, 0, TAU);
          ctx.stroke();
          this.sparks(f, 6, 2 + f.t * 10, 0.8, `rgba(200,255,250,${a})`, 0.25);
          break;
        }
        case "boss-kill": {
          const r = 1 + f.t * 14;
          ctx.strokeStyle = `rgba(${P.dangerRgb},${a * 0.8})`;
          ctx.lineWidth = 0.6;
          ctx.beginPath();
          ctx.arc(f.x, f.y, r, 0, TAU);
          ctx.stroke();
          ctx.strokeStyle = `rgba(255,255,255,${a * 0.5})`;
          ctx.lineWidth = 0.3;
          ctx.beginPath();
          ctx.arc(f.x, f.y, r * 0.6, 0, TAU);
          ctx.stroke();
          this.sparks(f, 12, 3 + f.t * 18, 1.2, `rgba(${P.dangerRgb},${a})`, 0.35);
          break;
        }
        case "crit": {
          if (f.t > FADE.crit) break;
          const k = 1 - f.t / FADE.crit;
          const y = f.y - 2 - f.t * 6;
          const txt = Math.round(f.value ?? 0).toString();
          ctx.font = `700 3.2px ${FONT}`;
          ctx.lineWidth = 0.5;
          ctx.strokeStyle = `rgba(0,0,0,${0.4 * k})`;
          ctx.strokeText(txt, f.x, y);
          ctx.fillStyle = `rgba(${P.amberRgb},${k})`;
          ctx.fillText(txt, f.x, y);
          break;
        }
        case "hit": {
          if (f.t > FADE.hit) break;
          const k = 1 - f.t / FADE.hit;
          ctx.fillStyle = `rgba(${P.dangerRgb},${k * 0.8})`;
          ctx.beginPath();
          ctx.arc(f.x, f.y, 0.8 + f.t * 3, 0, TAU);
          ctx.fill();
          this.sparks(f, 3, 0.8 + f.t * 6, 0.5, `rgba(${P.dangerRgb},${k * 0.7})`, 0.2);
          break;
        }
        case "atp": {
          if (f.t > FADE.atp) break;
          const y = f.y - f.t * 8;
          const txt = `+${Math.round(f.value ?? 0)}`;
          ctx.font = `700 3.4px ${FONT}`;
          ctx.lineWidth = 0.5;
          ctx.strokeStyle = `rgba(0,0,0,${0.5 * (1 - f.t)})`;
          ctx.strokeText(txt, f.x, y);
          ctx.fillStyle = `rgba(${P.atpRgb},${1 - f.t})`;
          ctx.fillText(txt, f.x, y);
          break;
        }
      }
    }
  }
  /** Screen-space overlays (call with the identity transform): boss-kill white flash. */
  drawScreenFx(world, w, h) {
    let a = 0;
    for (const f of world.fx) {
      if (f.kind === "boss-kill" && f.t < FADE.bossFlash) a = Math.max(a, 0.08 * (1 - f.t / FADE.bossFlash));
    }
    if (a <= 0) return;
    this.ctx.fillStyle = `rgba(255,255,255,${a})`;
    this.ctx.fillRect(0, 0, w, h);
  }
  /** n radial sparks at deterministic angles (seeded by position), flying outward. */
  sparks(f, n, dist, len, style, lw) {
    const { ctx } = this;
    const seed = Math.round(f.x * 7 + f.y * 13) | 0;
    ctx.beginPath();
    for (let i = 0; i < n; i++) {
      const a = hash01(i, seed) * TAU;
      const c = Math.cos(a);
      const s = Math.sin(a);
      ctx.moveTo(f.x + c * (dist - len), f.y + s * (dist - len));
      ctx.lineTo(f.x + c * dist, f.y + s * dist);
    }
    ctx.lineCap = "round";
    ctx.lineWidth = lw;
    ctx.strokeStyle = style;
    ctx.stroke();
    ctx.lineCap = "butt";
  }
}
export {
  FxPainter
};
