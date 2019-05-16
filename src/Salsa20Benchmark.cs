using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using benchmark.utils;
using BenchmarkDotNet.Attributes;

namespace benchmarks
{
    public class Salsa20Benchmark
    {
        [Benchmark]
        [BenchmarkCategory("PRNG")]
        public void GenerateRandom()
        {

            for (int v = 0; v < 10; v++)
            {
                byte[] rng = new byte[Program.RNG_COUNT * Program.INT_SIZE];
                using (SymmetricAlgorithm salsa20 = new Salsa20())
                using (ICryptoTransform encrypt = salsa20.CreateEncryptor(Guid.NewGuid().ToByteArray(),
                    Guid.NewGuid().ToByteArray().Take(8).ToArray()))
                using (MemoryStream streamInput = new MemoryStream(rng, false))
                using (CryptoStream streamEncrypted = new CryptoStream(streamInput, encrypt, CryptoStreamMode.Read))
                {
                    streamEncrypted.ReadAsync(rng).GetAwaiter().GetResult();
                }
            }
        }
    }
}
