using System;
using System.Linq;
using benchmark.utils;
using BenchmarkDotNet.Attributes;

namespace benchmarks
{
    public class ChaCha20Benchmark
    {
        [Benchmark]
        [BenchmarkCategory("PRNG")]
        public void GenerateRandom()
        {
            byte[] rngBytes = new byte[Program.RNG_COUNT * Program.INT_SIZE];
            ChaCha20 chaCha20 = new ChaCha20 { IsParallel = false };
            var keyParam = new KeyParams(Guid.NewGuid().ToByteArray(),
                Guid.NewGuid().ToByteArray().Take(8).ToArray());
            chaCha20.Initialize(keyParam);
            chaCha20.Transform(rngBytes, rngBytes);

            chaCha20.Dispose();
        }
    }
}
