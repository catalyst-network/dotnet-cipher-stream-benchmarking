using System;
using benchmark.utils;
using BenchmarkDotNet.Attributes;

namespace benchmarks
{
    public class IsaacBenchmark
    {
        [Benchmark]
        [BenchmarkCategory("PRNG")]
        public void GenerateRandom()
        {
            IsaacRandom random = new IsaacRandom(Guid.NewGuid().ToString());
            for (int i = 0; i < Program.RNG_COUNT; i++)
            {
                random.NextInt();
            }
        }
    }
}
