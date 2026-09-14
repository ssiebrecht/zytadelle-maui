// The few balance values the painters need at module load: the dish radius, the cell radius, and
// the canonical radius per pathogen, which sizes the gradients each species kit caches once.
//
// The source of truth is C# (Zytadelle.Balancing: ArenaBalance, EnemyBalance). These are a mirror,
// because a gradient cannot wait for the first frame to be built. arena.js checks them against the
// values the host reports at boot and complains loudly if they ever drift apart.

export const ARENA_RADIUS = 82;
export const CELL_RADIUS = 4;

export const ENEMIES = {
  basic: { radius: 2.2 },
  fast: { radius: 1.9 },
  tank: { radius: 3.6 },
  ranged: { radius: 2.2 },
  boss: { radius: 5.5 },
};

/** Order must match the EnemyKind enum in C#; the snapshot carries the kind as its index. */
export const KIND_NAMES = ['basic', 'fast', 'tank', 'ranged', 'boss'];

/** Order must match the FxKind enum in C#. */
export const FX_NAMES = ['kill', 'crit', 'boss-kill', 'hit', 'atp'];

export function checkConstants(host) {
  const mine = { arenaRadius: ARENA_RADIUS, cellRadius: CELL_RADIUS };
  for (const [k, v] of Object.entries(mine)) {
    if (Math.abs(host[k] - v) > 1e-9) {
      console.error(`renderer constant ${k} is ${v} but the host says ${host[k]} - update constants.js`);
    }
  }
  KIND_NAMES.forEach((name, i) => {
    const there = host.enemyRadii?.[i];
    if (there !== undefined && Math.abs(there - ENEMIES[name].radius) > 1e-9) {
      console.error(`renderer radius for ${name} is ${ENEMIES[name].radius} but the host says ${there}`);
    }
  });
}
