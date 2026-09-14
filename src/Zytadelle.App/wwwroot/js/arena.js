// Host side of the arena: owns the canvas, drives the frame clock, and turns the packed snapshot
// the C# side returns back into the plain object shape the painters already expect.
//
// The browser drives: one requestAnimationFrame callback per displayed frame asks C# to advance the
// simulation and hand back a snapshot. That is the minimum possible interop - one round trip per
// frame - and it gives back-pressure for free, because the next frame is only requested once the
// previous one has landed. It also stops on its own when the window is hidden, which is exactly what
// an idle game should do.

import { CanvasRenderer } from './render/canvasRenderer.js';
import { KIND_NAMES, FX_NAMES, checkConstants } from './render/constants.js';

// Must match Zytadelle.Core.Snapshot.FrameEncoder.
const HEADER = 40;
const ENEMY = 44;
const PROJ = 20;
const FX = 20;

let api = null;

class Arena {
  constructor(canvas) {
    this.canvas = canvas;
    this.renderer = new CanvasRenderer(canvas);
    this.dotnet = null;
    this.raf = 0;
    this.inflight = false;
    this.plate = null;
    // Reusable view objects: decoding must not allocate, or the collector runs every few frames.
    this.view = {
      time: 0,
      cell: { hp: 1, maxHp: 1, flash: 0, fireCd: 0 },
      stats: { range: 30, attackSpeed: 1 },
      enemies: [],
      projectiles: [],
      fx: [],
    };
    this.enemyPool = [];
    this.projPool = [];
    this.fxPool = [];
    this.perf = { frames: 0, intervals: [], latencies: [], last: 0 };
  }

  grow(pool, n, make) {
    while (pool.length < n) pool.push(make());
    return pool;
  }

  decode(bytes) {
    const dv = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    const v = this.view;

    v.time = dv.getFloat64(0, true);
    v.cell.hp = dv.getFloat32(8, true);
    v.cell.maxHp = dv.getFloat32(12, true);
    v.cell.flash = dv.getFloat32(16, true);
    v.cell.fireCd = dv.getFloat32(20, true);
    v.stats.range = dv.getFloat32(24, true);
    v.stats.attackSpeed = dv.getFloat32(28, true);

    const nE = dv.getUint16(32, true);
    const nP = dv.getUint16(34, true);
    const nF = dv.getUint16(36, true);

    this.grow(this.enemyPool, nE, () => ({
      id: 0, kind: 'basic', x: 0, y: 0, radius: 1, hp: 1, maxHp: 1,
      flash: 0, dmgMult: 1, attackCd: 0, arrived: false, alive: true,
      def: { attackInterval: 2 },
    }));
    this.grow(this.projPool, nP, () => ({ x: 0, y: 0, vx: 0, vy: 0, crit: false, fromCell: true, alive: true }));
    this.grow(this.fxPool, nF, () => ({ kind: 'kill', x: 0, y: 0, t: 0, value: 0 }));

    let o = HEADER;
    v.enemies.length = nE;
    for (let i = 0; i < nE; i++) {
      const e = this.enemyPool[i];
      e.id = dv.getInt32(o, true);
      e.kind = KIND_NAMES[dv.getUint8(o + 4)];
      e.arrived = dv.getUint8(o + 5) !== 0;
      e.x = dv.getFloat32(o + 8, true);
      e.y = dv.getFloat32(o + 12, true);
      e.radius = dv.getFloat32(o + 16, true);
      e.hp = dv.getFloat32(o + 20, true);
      e.maxHp = dv.getFloat32(o + 24, true);
      e.flash = dv.getFloat32(o + 28, true);
      e.dmgMult = dv.getFloat32(o + 32, true);
      e.attackCd = dv.getFloat32(o + 36, true);
      e.def.attackInterval = dv.getFloat32(o + 40, true);
      v.enemies[i] = e;
      o += ENEMY;
    }

    v.projectiles.length = nP;
    for (let i = 0; i < nP; i++) {
      const p = this.projPool[i];
      p.x = dv.getFloat32(o, true);
      p.y = dv.getFloat32(o + 4, true);
      p.vx = dv.getFloat32(o + 8, true);
      p.vy = dv.getFloat32(o + 12, true);
      const flags = dv.getUint8(o + 16);
      p.crit = (flags & 1) !== 0;
      p.fromCell = (flags & 2) !== 0;
      v.projectiles[i] = p;
      o += PROJ;
    }

    v.fx.length = nF;
    for (let i = 0; i < nF; i++) {
      const f = this.fxPool[i];
      f.kind = FX_NAMES[dv.getUint8(o)];
      f.x = dv.getFloat32(o + 4, true);
      f.y = dv.getFloat32(o + 8, true);
      f.t = dv.getFloat32(o + 12, true);
      f.value = dv.getFloat32(o + 16, true);
      v.fx[i] = f;
      o += FX;
    }

    return v;
  }

  /**
   * The plate showing integrity floats over the canvas, so its strip is not dish space. Measured
   * here rather than passed in, because it is a pure layout question the host has no opinion on.
   */
  measureInset() {
    const p = document.querySelector('.hp-plate')?.getBoundingClientRect();
    if (!p || !p.height) return 0;
    return Math.max(0, this.canvas.getBoundingClientRect().bottom - p.top + 12);
  }

  /**
   * Blazor lays the shell out and leaves an empty slot where the arena belongs; the canvas layer
   * follows that slot. Keeping the canvas out of the component tree is what guarantees no re-render
   * can ever replace it, and mirroring one rectangle is cheaper than paying for that guarantee in
   * render-lock discipline.
   */
  mirrorSlot() {
    const slot = document.getElementById('arena-slot');
    const layer = document.getElementById('arena-layer');
    if (!slot || !layer) return;
    const r = slot.getBoundingClientRect();
    const s = layer.style;
    s.left = `${r.left}px`;
    s.top = `${r.top}px`;
    s.width = `${r.width}px`;
    s.height = `${r.height}px`;
  }

  resize() {
    this.mirrorSlot();
    const inset = this.measureInset();
    if (inset !== this.renderer.bottomInset) this.renderer.bottomInset = inset;
    this.renderer.resize();
  }

  setZoom(zoom) {
    if (zoom === this.renderer.zoom) return;
    this.renderer.zoom = zoom;
    this.resize();
  }

  frame = (now) => {
    this.raf = requestAnimationFrame(this.frame);
    if (this.perf.last) this.perf.intervals.push(now - this.perf.last);
    this.perf.last = now;
    if (this.inflight || document.hidden || !this.dotnet) return;

    this.inflight = true;
    const t0 = performance.now();
    this.dotnet.invokeMethodAsync('Tick', now)
      .then((result) => {
        this.perf.latencies.push(performance.now() - t0);
        if (!result) return;
        const bytes = typeof result === 'string' ? base64ToBytes(result) : result;
        this.renderer.render(this.decode(bytes), 0);
        this.perf.frames++;
      })
      .catch((err) => console.error('tick failed', err))
      .finally(() => { this.inflight = false; });
  };

  start() {
    if (this.raf) return;
    this.perf.last = 0;
    this.raf = requestAnimationFrame(this.frame);
  }

  stop() {
    if (this.raf) cancelAnimationFrame(this.raf);
    this.raf = 0;
  }
}

function base64ToBytes(s) {
  const bin = atob(s);
  const out = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; i++) out[i] = bin.charCodeAt(i);
  return out;
}

/** Idempotent: hot reload re-runs the module, and a second loop would double the frame rate. */
export async function boot(canvasSelector, dotnet, hostConstants) {
  const canvas = document.querySelector(canvasSelector);
  if (!canvas) throw new Error(`no canvas at ${canvasSelector}`);

  if (canvas.__zytadelle) {
    api = canvas.__zytadelle;
    api.dotnet = dotnet;
    return;
  }

  if (hostConstants) checkConstants(hostConstants);

  // The canvas measures text; a font swapping in mid-game would reflow every HUD label it draws.
  if (document.fonts?.ready) await document.fonts.ready;

  api = new Arena(canvas);
  api.dotnet = dotnet;
  canvas.__zytadelle = api;

  const observer = new ResizeObserver(() => api.resize());
  observer.observe(document.body);
  const slot = document.getElementById('arena-slot');
  if (slot) observer.observe(slot);
  api.observer = observer;
  window.addEventListener('resize', () => api.resize());

  api.resize();
  api.start();
}

export function setZoom(zoom) {
  api?.setZoom(zoom);
}

/** Called after a layout change in Blazor; also picks up the slot if it appeared after boot. */
export function relayout() {
  if (!api) return;
  const slot = document.getElementById('arena-slot');
  if (slot && api.observedSlot !== slot) {
    api.observer?.observe(slot);
    api.observedSlot = slot;
  }
  api.resize();
}

/** Frame budget probe: median interval, p95 interop latency, completed frames per second. */
export function perfReport(reset = true) {
  if (!api) return null;
  const pct = (arr, p) => {
    if (!arr.length) return 0;
    const s = [...arr].sort((a, b) => a - b);
    return s[Math.min(s.length - 1, Math.floor(s.length * p))];
  };
  const { intervals, latencies, frames } = api.perf;
  const spanMs = intervals.reduce((a, b) => a + b, 0);
  const slot = document.getElementById('arena-slot')?.getBoundingClientRect();
  const report = {
    frames,
    fps: spanMs > 0 ? (frames * 1000) / spanMs : 0,
    innerWidth: window.innerWidth,
    innerHeight: window.innerHeight,
    dpr: window.devicePixelRatio || 1,
    slot: slot ? `${Math.round(slot.left)},${Math.round(slot.top)} ${Math.round(slot.width)}x${Math.round(slot.height)}` : '-',
    enemies: api.view.enemies.length,
    intervalMedian: pct(intervals, 0.5),
    intervalP95: pct(intervals, 0.95),
    latencyMedian: pct(latencies, 0.5),
    latencyP95: pct(latencies, 0.95),
    renderMs: api.renderer.lastMs,
  };
  if (reset) api.perf = { frames: 0, intervals: [], latencies: [], last: api.perf.last };
  return report;
}
