/*
 * Small canvas line chart - enough axes, ticks, hover and a log toggle to read a balance curve, and
 * nothing more. Ported from the browser build's Balance Lab, which hand-drew these rather than pull
 * in a chart library for three plots; the same reasoning holds here.
 *
 * Blazor only ever hands over a finished series and walks away. Drawing happens on resize and on a
 * new series, never per frame, so there is no clock on this side at all.
 */
const charts = new Map();

const PAD = { left: 62, right: 12, top: 12, bottom: 30 };
const css = (el, name, fallback) => getComputedStyle(el).getPropertyValue(name).trim() || fallback;

class LineChart {
  constructor(host, opts) {
    this.host = host;
    this.opts = opts;
    this.series = [];
    this.logY = false;
    this.hover = null;

    this.canvas = document.createElement('canvas');
    this.canvas.className = 'chart-canvas';
    this.tip = document.createElement('div');
    this.tip.className = 'chart-tip';
    this.tip.hidden = true;
    host.replaceChildren(this.canvas, this.tip);
    this.ctx = this.canvas.getContext('2d');

    this.ro = new ResizeObserver(() => this.draw());
    this.ro.observe(host);
    this.canvas.addEventListener('pointermove', (e) => this.onMove(e));
    this.canvas.addEventListener('pointerleave', () => {
      this.hover = null;
      this.tip.hidden = true;
      this.draw();
    });
  }

  setSeries(series) {
    this.series = series ?? [];
    this.hover = null;
    this.tip.hidden = true;
    this.draw();
  }

  setLogY(on) {
    this.logY = on;
    this.draw();
  }

  destroy() {
    this.ro.disconnect();
    this.host.replaceChildren();
  }

  bounds() {
    let x0 = Infinity, x1 = -Infinity, y0 = Infinity, y1 = -Infinity;
    for (const s of this.series) {
      for (const p of s.points) {
        if (!Number.isFinite(p.x) || !Number.isFinite(p.y)) continue;
        if (this.logY && p.y <= 0) continue;
        x0 = Math.min(x0, p.x); x1 = Math.max(x1, p.x);
        y0 = Math.min(y0, p.y); y1 = Math.max(y1, p.y);
      }
    }
    if (!Number.isFinite(x0) || !Number.isFinite(y0)) return null;
    if (x1 === x0) x1 = x0 + 1;
    if (y1 === y0) y1 = y0 + Math.max(1e-9, Math.abs(y0) * 0.1);
    if (!this.logY) y0 = Math.min(y0, 0);
    return { x0, x1, y0, y1 };
  }

  scales(rect, b) {
    const plotW = rect.width - PAD.left - PAD.right;
    const plotH = rect.height - PAD.top - PAD.bottom;
    const ty = (v) => (this.logY ? Math.log10(Math.max(v, 1e-12)) : v);
    const ly = ty(b.y0), hy = ty(b.y1);
    return {
      plotW,
      plotH,
      sx: (x) => PAD.left + ((x - b.x0) / (b.x1 - b.x0)) * plotW,
      sy: (y) => PAD.top + plotH - ((ty(y) - ly) / (hy - ly || 1)) * plotH,
    };
  }

  draw() {
    const dpr = window.devicePixelRatio || 1;
    const w = this.host.clientWidth;
    const h = this.host.clientHeight;
    if (w < 8 || h < 8) return;
    this.canvas.width = Math.round(w * dpr);
    this.canvas.height = Math.round(h * dpr);
    this.canvas.style.width = `${w}px`;
    this.canvas.style.height = `${h}px`;

    const c = this.ctx;
    c.setTransform(dpr, 0, 0, dpr, 0, 0);
    c.clearRect(0, 0, w, h);

    const ink = css(this.host, '--text-mute', '#6b8c89');
    const line = css(this.host, '--line-soft', 'rgba(255,255,255,.06)');
    c.font = '11px Oxanium, system-ui, sans-serif';

    const b = this.bounds();
    if (!b) {
      c.fillStyle = ink;
      c.textAlign = 'center';
      c.fillText('no data', w / 2, h / 2);
      return;
    }

    const { plotW, plotH, sx, sy } = this.scales({ width: w, height: h }, b);

    c.strokeStyle = line;
    c.fillStyle = ink;
    c.lineWidth = 1;
    for (const v of ticks(b.y0, b.y1, this.logY)) {
      const y = Math.round(sy(v)) + 0.5;
      if (y < PAD.top || y > PAD.top + plotH) continue;
      c.beginPath();
      c.moveTo(PAD.left, y);
      c.lineTo(w - PAD.right, y);
      c.stroke();
      c.textAlign = 'right';
      c.textBaseline = 'middle';
      c.fillText(format(v), PAD.left - 6, y);
    }
    c.textAlign = 'center';
    c.textBaseline = 'top';
    for (const v of ticks(b.x0, b.x1, false)) {
      const x = Math.round(sx(v)) + 0.5;
      if (x < PAD.left || x > w - PAD.right) continue;
      c.beginPath();
      c.moveTo(x, PAD.top);
      c.lineTo(x, PAD.top + plotH);
      c.stroke();
      c.fillText(String(Math.round(v)), x, PAD.top + plotH + 6);
    }
    c.textAlign = 'right';
    c.fillText(this.opts.xLabel, w - PAD.right, PAD.top + plotH + 16);
    c.save();
    c.translate(12, PAD.top + plotH / 2);
    c.rotate(-Math.PI / 2);
    c.textAlign = 'center';
    c.textBaseline = 'top';
    c.fillText(this.opts.yLabel, 0, 0);
    c.restore();

    for (const s of this.series) {
      if (s.points.length === 0) continue;
      c.strokeStyle = s.color;
      c.lineWidth = 1.6;
      c.setLineDash(s.dashed ? [5, 4] : []);
      c.beginPath();
      let lastY = null;
      for (const p of s.points) {
        if (this.logY && p.y <= 0) continue;
        const x = sx(p.x), y = sy(p.y);
        if (lastY === null) c.moveTo(x, y);
        else if (this.opts.step) { c.lineTo(x, lastY); c.lineTo(x, y); }
        else c.lineTo(x, y);
        lastY = y;
      }
      c.stroke();
    }
    c.setLineDash([]);

    if (this.hover) {
      c.fillStyle = this.hover.s.color;
      c.beginPath();
      c.arc(sx(this.hover.p.x), sy(this.hover.p.y), 3.5, 0, Math.PI * 2);
      c.fill();
    }
  }

  onMove(e) {
    const rect = this.canvas.getBoundingClientRect();
    const b = this.bounds();
    if (!b) return;
    const { sx, sy } = this.scales(rect, b);
    const mx = e.clientX - rect.left;
    const my = e.clientY - rect.top;

    let best = null;
    for (const s of this.series) {
      for (const p of s.points) {
        if (this.logY && p.y <= 0) continue;
        const d = Math.hypot(sx(p.x) - mx, sy(p.y) - my);
        if (!best || d < best.d) best = { s, p, d };
      }
    }
    if (!best || best.d > 26) {
      if (this.hover) {
        this.hover = null;
        this.tip.hidden = true;
        this.draw();
      }
      return;
    }
    this.hover = { s: best.s, p: best.p };
    this.tip.hidden = false;
    this.tip.textContent = `${best.s.label} · ${this.opts.xLabel} ${Math.round(best.p.x)} · ${format(best.p.y)}`;
    this.tip.style.left = `${Math.min(rect.width - this.tip.offsetWidth - 6, Math.max(4, sx(best.p.x) + 10))}px`;
    this.tip.style.top = `${Math.max(4, sy(best.p.y) - 34)}px`;
    this.draw();
  }
}

/** Round tick values: 1/2/5 times a power of ten, or whole decades on a log axis. */
function ticks(lo, hi, log) {
  if (log) {
    const a = Math.floor(Math.log10(Math.max(lo, 1e-12)));
    const b = Math.ceil(Math.log10(Math.max(hi, 1e-12)));
    const out = [];
    for (let e = a; e <= b && out.length < 12; e++) out.push(10 ** e);
    return out;
  }
  const span = hi - lo;
  if (!(span > 0)) return [lo];
  const raw = span / 5;
  const mag = 10 ** Math.floor(Math.log10(raw));
  const norm = raw / mag;
  const step = (norm >= 5 ? 10 : norm >= 2 ? 5 : norm >= 1 ? 2 : 1) * mag;
  const out = [];
  for (let v = Math.ceil(lo / step) * step; v <= hi + step * 1e-9 && out.length < 20; v += step) out.push(v);
  return out;
}

const format = (v) =>
  Math.abs(v) >= 1000 ? `${(v / 1000).toFixed(1)}k` : Math.abs(v) >= 10 ? v.toFixed(0) : v.toFixed(2);

window.labChart = {
  draw(id, series, opts) {
    const host = document.getElementById(id);
    if (!host) return;
    let chart = charts.get(id);
    if (!chart) {
      chart = new LineChart(host, opts);
      charts.set(id, chart);
    } else {
      chart.opts = opts;
    }
    chart.setSeries(series);
  },
  setLog(id, on) {
    charts.get(id)?.setLogY(on);
  },
  destroy(id) {
    charts.get(id)?.destroy();
    charts.delete(id);
  },
};
