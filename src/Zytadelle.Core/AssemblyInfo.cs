using System.Runtime.CompilerServices;

// The determinism probes read Rng.State directly, so the test assembly needs access to internals;
// nothing else outside Core should reach for them.
[assembly: InternalsVisibleTo("Zytadelle.Core.Tests")]
