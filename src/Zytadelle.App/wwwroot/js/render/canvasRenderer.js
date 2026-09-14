import { ARENA_RADIUS } from "./constants.js";
import { Backdrop } from "./backdrop.js";
import { CellPainter } from "./cell.js";
import { EnemyPainter } from "./enemies.js";
import { FxPainter } from "./fx.js";
class CanvasRenderer {
  constructor(canvas) {
    this.canvas = canvas;
    const ctx = canvas.getContext("2d", { alpha: false });
    if (!ctx) throw new Error("2d context unavailable");
    this.ctx = ctx;
    this.cell = new CellPainter(ctx);
    this.enemies = new EnemyPainter(ctx);
    this.fx = new FxPainter(ctx);
    this.resize();
  }
  canvas;
  ctx;
  w = 0;
  h = 0;
  dpr = 1;
  /** px per meter */
  scale = 1;
  /** 1 = whole dish fits; > 1 zooms onto the cell (hub preview). Takes effect on resize(). */
  zoom = 1;
  /** px of the canvas bottom owned by an overlay (the integrity plate): the dish centres above it. */
  bottomInset = 0;
  /** duration of the last render() in ms (dev perf probe) */
  lastMs = 0;
  cx = 0;
  cy = 0;
  backdrop = new Backdrop();
  cell;
  enemies;
  fx;
  resize() {
    const rect = this.canvas.getBoundingClientRect();
    this.dpr = Math.min(2, window.devicePixelRatio || 1);
    this.w = Math.max(1, Math.floor(rect.width));
    this.h = Math.max(1, Math.floor(rect.height));
    this.canvas.width = Math.floor(this.w * this.dpr);
    this.canvas.height = Math.floor(this.h * this.dpr);
    const usable = Math.max(1, this.h - this.bottomInset);
    this.cx = this.w / 2;
    this.cy = usable / 2;
    const half = Math.max(1, Math.min(this.w, usable) * 0.5 - 12);
    this.scale = half / (ARENA_RADIUS * 1.04) * this.zoom;
    this.backdrop.rebuild(this.w, this.h, this.dpr, ARENA_RADIUS * this.scale, this.cx, this.cy);
  }
  render(world, _dtVisual) {
    const t0 = performance.now();
    const { ctx } = this;
    ctx.setTransform(this.dpr, 0, 0, this.dpr, 0, 0);
    this.backdrop.draw(ctx, world, this.w, this.h, this.cx, this.cy);
    ctx.save();
    ctx.translate(this.cx, this.cy);
    ctx.scale(this.scale, this.scale);
    this.cell.drawRange(world);
    this.enemies.draw(world);
    this.fx.drawProjectiles(world);
    this.cell.draw(world);
    this.fx.drawFx(world);
    ctx.restore();
    this.fx.drawScreenFx(world, this.w, this.h);
    this.lastMs = performance.now() - t0;
  }
}
export {
  CanvasRenderer
};
