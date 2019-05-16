``` ini

BenchmarkDotNet=v0.11.3, OS=Windows 10.0.17763.437 (1809/October2018Update/Redstone5)
Intel Core i7-7700HQ CPU 2.80GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=2.2.203
  [Host]     : .NET Core 2.1.9 (CoreCLR 4.6.27414.06, CoreFX 4.6.27415.01), 64bit RyuJIT
  DefaultJob : .NET Core 2.1.9 (CoreCLR 4.6.27414.06, CoreFX 4.6.27415.01), 64bit RyuJIT


```
|         Method |     Mean |    Error |   StdDev |
|--------------- |---------:|---------:|---------:|
| GenerateRandom | 723.4 us | 9.020 us | 7.532 us |
