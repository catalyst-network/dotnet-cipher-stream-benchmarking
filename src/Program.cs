using BenchmarkDotNet.Running;

namespace benchmarks
{
    public class Program
    {
        public static int RNG_COUNT = 100000;
        public static int INT_SIZE = 4;

        public static void Main(string[] args)
        {   
           BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }

    }
}

