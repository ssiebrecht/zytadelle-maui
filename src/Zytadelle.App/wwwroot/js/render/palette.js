const P = {
  bg: "#050a0f",
  fieldCenter: "#0b1c24",
  fieldMid: "#06111a",
  fieldEdge: "#03080c",
  cyan: "#6ff2e2",
  cyanRgb: "111,242,226",
  violet: "#b48cff",
  violetRgb: "180,140,255",
  pink: "#e88ab8",
  pinkRgb: "232,138,184",
  magentaRgb: "255,110,200",
  atp: "#7dff9a",
  atpRgb: "125,255,154",
  amber: "#ffc85c",
  amberRgb: "255,200,92",
  danger: "#ff5470",
  dangerRgb: "255,84,112",
  white: "#ffffff",
  enemy: {
    basic: "#ff6d82",
    // crimson-coral rod (the inspiration's red bacillus)
    fast: "#ffb547",
    // amber spirochete
    tank: "#b9f05c",
    // acid-green staph cluster (biofilm)
    ranged: "#d66cff",
    // magenta-violet phage
    boss: "#ff3552"
    // blood-red ciliate
  }
};
const FONT = 'Oxanium, Bahnschrift, "Segoe UI", sans-serif';
function hexRgb(hex) {
  const n = parseInt(hex.slice(1), 16);
  return `${n >> 16 & 255},${n >> 8 & 255},${n & 255}`;
}
function rgba(hex, a) {
  return `rgba(${hexRgb(hex)},${a})`;
}
function mixRgb(a, b, t) {
  const na = parseInt(a.slice(1), 16);
  const nb = parseInt(b.slice(1), 16);
  const ch = (s) => Math.round((na >> s & 255) * (1 - t) + (nb >> s & 255) * t);
  return `${ch(16)},${ch(8)},${ch(0)}`;
}
function tintRgb(hex, t) {
  return mixRgb(hex, "#ffffff", t);
}
function shadeRgb(hex, t) {
  return mixRgb(hex, "#000000", t);
}
function radial(ctx, r, stops, x = 0, y = 0, r0 = 0) {
  const g = ctx.createRadialGradient(x, y, r0, x, y, r);
  for (const [o, c] of stops) g.addColorStop(o, c);
  return g;
}
function linear(ctx, x0, y0, x1, y1, stops) {
  const g = ctx.createLinearGradient(x0, y0, x1, y1);
  for (const [o, c] of stops) g.addColorStop(o, c);
  return g;
}
function glowStops(rgb, a) {
  return [
    [0, `rgba(${rgb},${a})`],
    [0.45, `rgba(${rgb},${a * 0.38})`],
    [1, `rgba(${rgb},0)`]
  ];
}
export {
  FONT,
  P,
  glowStops,
  hexRgb,
  linear,
  mixRgb,
  radial,
  rgba,
  shadeRgb,
  tintRgb
};
