// The balance values the painters read. None of them are written here: the host hands the whole set
// over once, in `boot`, and `applyBalance` spreads it across these bindings before the first painter
// is constructed. The source of truth is C# (Zytadelle.Balancing plus FrameEncoder for the layout),
// and there is no second copy to drift from it.
//
// These are `let`, not `const`, on purpose - module exports are live bindings, so every importer
// sees the values the host reported without having to be handed them.

/** Radius of the dish. */
export let ARENA_RADIUS = 0;

/** Radius of the player cell. */
export let CELL_RADIUS = 0;

/** Slack around the dish so its rim is never flush against the canvas edge. */
export let CAMERA_PAD = 1;

/** Seconds an effect stays in the ring; every per-effect fade is shorter than this. */
export let FX_LIFETIME = 0;

/** Seconds a pathogen shows its hit flash. */
export let ENEMY_FLASH = 0;

/** Integrity fraction below which the cell reads as in danger. */
export let LOW_INTEGRITY = 0;

/** A pathogen shows a health bar only below this much of its own health. */
export let HP_BAR_HIDE_ABOVE = 0;

/** Damage multiplier from which a heated-up pathogen glows. */
export let HEAT_GLOW_FROM = Infinity;

/** Seconds each kind of effect is actually drawn for. */
export const FADE = { crit: 0, hit: 0, atp: 0, bossFlash: 0 };

/** Strides of the packed frame, straight from FrameEncoder. */
export const FRAME = { header: 0, enemy: 0, projectile: 0, fx: 0 };

/** Canonical radius per pathogen; sizes the gradients each species kit caches once. */
export const ENEMIES = {
  basic: { radius: 0 },
  fast: { radius: 0 },
  tank: { radius: 0 },
  ranged: { radius: 0 },
  boss: { radius: 0 },
};

/** What the view shows in the one frame before the first snapshot lands. */
export const SEED = { range: 0, attackSpeed: 0, attackInterval: 0 };

/** Order must match the EnemyKind enum in C#; the snapshot carries the kind as its index. */
export const KIND_NAMES = ['basic', 'fast', 'tank', 'ranged', 'boss'];

/** Order must match the FxKind enum in C#. */
export const FX_NAMES = ['kill', 'crit', 'boss-kill', 'hit', 'atp'];

function need(host, path) {
  const v = path.split('.').reduce((o, k) => (o == null ? undefined : o[k]), host);
  if (typeof v !== 'number' || !Number.isFinite(v)) {
    throw new Error(`arena boot: the host did not report ${path}`);
  }
  return v;
}

/**
 * Takes the balance payload the host sends at boot. Must run before any painter is constructed,
 * because the species kits bake the radii into cached gradients the moment they are built.
 */
export function applyBalance(host) {
  if (!host) throw new Error('arena boot: no balance payload');

  ARENA_RADIUS = need(host, 'arenaRadius');
  CELL_RADIUS = need(host, 'cellRadius');
  CAMERA_PAD = need(host, 'cameraPad');
  FX_LIFETIME = need(host, 'fxLifetime');
  ENEMY_FLASH = need(host, 'enemyFlash');
  LOW_INTEGRITY = need(host, 'lowIntegrityFrac');
  HP_BAR_HIDE_ABOVE = need(host, 'hpBarHideAbove');
  HEAT_GLOW_FROM = need(host, 'heatGlowFrom');

  FADE.crit = need(host, 'fade.crit');
  FADE.hit = need(host, 'fade.hit');
  FADE.atp = need(host, 'fade.atp');
  FADE.bossFlash = need(host, 'fade.bossFlash');

  // A zero stride would walk the buffer in place and decode the same bytes forever, so the frame
  // layout is the one thing worth refusing to start without.
  FRAME.header = need(host, 'frame.header');
  FRAME.enemy = need(host, 'frame.enemy');
  FRAME.projectile = need(host, 'frame.projectile');
  FRAME.fx = need(host, 'frame.fx');
  for (const [k, v] of Object.entries(FRAME)) {
    if (v <= 0) throw new Error(`arena boot: frame stride ${k} is ${v}`);
  }

  KIND_NAMES.forEach((name, i) => {
    const r = host.enemyRadii?.[i];
    if (typeof r !== 'number' || !(r > 0)) {
      throw new Error(`arena boot: the host did not report a radius for ${name}`);
    }
    ENEMIES[name].radius = r;
  });

  SEED.range = need(host, 'seed.range');
  SEED.attackSpeed = need(host, 'seed.attackSpeed');
  SEED.attackInterval = need(host, 'seed.attackInterval');
}
