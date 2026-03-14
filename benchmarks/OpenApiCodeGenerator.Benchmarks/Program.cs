using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace OpenApiCodeGenerator.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        IConfig config = DefaultConfig.Instance;

        BenchmarkRunner.Run<ComparisonBenchmarks>(config, args);
    }
}
