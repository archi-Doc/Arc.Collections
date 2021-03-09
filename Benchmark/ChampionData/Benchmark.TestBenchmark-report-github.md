``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.200
  [Host]    : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT  [AttachedDebugger]
  MediumRun : .NET Core 5.0.3 (CoreCLR 5.0.321.7212, CoreFX 5.0.321.7212), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|        Method | Length |           Mean |       Error |       StdDev |   Gen 0 |   Gen 1 | Gen 2 | Allocated |
|-------------- |------- |---------------:|------------:|-------------:|--------:|--------:|------:|----------:|
|  EnumerateRef |  10000 |       411.2 ns |     2.80 ns |      4.10 ns |  0.0916 |       - |     - |     384 B |
|     Enumerate |  10000 |       210.4 ns |     1.87 ns |      2.62 ns |  0.0229 |       - |     - |      96 B |
|  NewAndAddRef |  10000 | 1,353,968.8 ns | 7,055.84 ns |  9,419.34 ns | 82.0313 | 31.2500 |     - |  400328 B |
|     NewAndAdd |  10000 | 1,268,137.0 ns | 7,885.63 ns | 11,558.65 ns | 80.0781 | 39.0625 |     - |  480376 B |
|  AddRemoveRef |  10000 |       860.3 ns |    15.42 ns |     22.61 ns |  0.0381 |       - |     - |     160 B |
|     AddRemove |  10000 |       476.3 ns |     3.15 ns |      4.42 ns |  0.0458 |       - |     - |     192 B |
| AddRemoveNode |  10000 |       257.9 ns |     3.45 ns |      4.95 ns |  0.0458 |       - |     - |     192 B |
