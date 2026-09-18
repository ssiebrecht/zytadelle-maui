using BenchmarkDotNet.Running;

using Zytadelle.Benchmarks;

switch (args.FirstOrDefault())
{
    case "--quick":
        QuickBench.Run(args[1..]);
        break;
    case "--loop":
        QuickBench.Loop(args[1..]);
        break;
    case "--compare":
        Compare.Run(args[1..]);
        break;
    default:
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        break;
}
