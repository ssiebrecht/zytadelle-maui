using Zytadelle.Core.Snapshot;

namespace Zytadelle.Core.Tests.Determinism;

/// <summary>
/// Everything <see cref="WorldProbe"/> deliberately leaves out because it is render-only: the Fx
/// ring, the income windows, and the exact bytes <see cref="FrameEncoder"/> would send to the
/// browser. Kept as a second hash so a change that is invisible to the simulation itself but would
/// still show up on screen - a reordered Fx write, a windowing off-by-one - gets caught without
/// forcing every field of it into the primary contract. Sampled at cycle checkpoints only:
/// re-encoding a full frame every tick would dominate golden-generation time for no benefit, since
/// nothing in <see cref="Zytadelle.Core.Sim.Step"/> reads any of this back.
/// </summary>
public static class RenderProbe
{
    public static void Write(World w, WordWriter buf)
    {
        buf.I32(w.Fx.Count);
        foreach (var f in w.Fx)
        {
            buf.I32((int)f.Kind);
            buf.F64(f.X);
            buf.F64(f.Y);
            buf.F64(f.T);
            buf.F64(f.Value);
        }

        buf.I32(w.AtpWindow.Count);
        foreach (var v in w.AtpWindow) buf.F64(v);
        buf.I32(w.DnaWindow.Count);
        foreach (var v in w.DnaWindow) buf.F64(v);
        buf.F64(Step.PerMinute(w.AtpWindow));
        buf.F64(Step.PerMinute(w.DnaWindow));

        var frame = FrameEncoder.Encode(w);
        buf.I32(frame.Length);
        buf.Bytes(frame);
    }
}
