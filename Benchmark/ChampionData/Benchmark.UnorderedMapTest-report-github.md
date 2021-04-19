``` ini

BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19042
Intel Core i7-6700K CPU 4.00GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.202
  [Host]    : .NET Core 5.0.5 (CoreCLR 5.0.521.16609, CoreFX 5.0.521.16609), X64 RyuJIT
  MediumRun : .NET Core 5.0.5 (CoreCLR 5.0.521.16609, CoreFX 5.0.521.16609), X64 RyuJIT

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
|                         Method | Count |      Mean |     Error |    StdDev |    Median |   Gen 0 |  Gen 1 | Gen 2 | Allocated |
|------------------------------- |------ |----------:|----------:|----------:|----------:|--------:|-------:|------:|----------:|
|      AddSerialInt_Dictionary__ |  1000 | 15.596 μs | 0.1683 μs | 0.2467 μs | 15.513 μs | 17.4255 | 1.9226 |     - |   73168 B |
|      AddSerialInt_DictionaryB_ |  1000 |  7.520 μs | 0.1003 μs | 0.1407 μs |  7.470 μs |  5.2872 | 0.3738 |     - |   22192 B |
| AddSerialInt_UnorderedMapClass |  1000 | 28.466 μs | 0.1958 μs | 0.2680 μs | 28.411 μs | 19.3176 | 3.2043 |     - |   80896 B |
|      AddSerialInt_UnorderedMap |  1000 | 17.562 μs | 0.1303 μs | 0.1910 μs | 17.508 μs | 11.7188 | 1.1597 |     - |   49192 B |
|     AddSerialInt_UnorderedMapB |  1000 | 12.822 μs | 0.0714 μs | 0.1024 μs | 12.804 μs |  5.8746 | 0.4425 |     - |   24712 B |
|     AddSerialInt_UnorderedMap2 |  1000 | 18.010 μs | 0.2071 μs | 0.3036 μs | 17.958 μs | 11.7188 | 1.1597 |     - |   49184 B |
|     AddSerialInt_UnorderedMap4 |  1000 | 19.412 μs | 0.1528 μs | 0.2191 μs | 19.326 μs | 12.1155 | 0.0305 |     - |   50912 B |
|      AddSerialInt_UnorderedSet |  1000 | 17.791 μs | 0.2967 μs | 0.4256 μs | 17.684 μs | 11.7188 | 1.0986 |     - |   49216 B |
|      AddRandomInt_Dictionary__ |  1000 | 16.054 μs | 1.1093 μs | 1.6260 μs | 15.249 μs |  8.2092 |      - |     - |   34496 B |
| AddRandomInt_UnorderedMapClass |  1000 | 35.761 μs | 0.6163 μs | 0.8436 μs | 35.424 μs | 15.1367 | 1.8921 |     - |   63424 B |
|      AddRandomInt_UnorderedMap |  1000 | 17.994 μs | 0.2099 μs | 0.2874 μs | 17.865 μs | 11.7188 | 1.1597 |     - |   49192 B |
|     AddRandomInt_UnorderedMap2 |  1000 | 19.395 μs | 0.3661 μs | 0.5133 μs | 19.294 μs | 11.7188 | 1.1597 |     - |   49184 B |
|     AddRandomInt_UnorderedMap4 |  1000 | 21.295 μs | 0.2581 μs | 0.3864 μs | 21.190 μs | 12.1155 | 0.0305 |     - |   50912 B |
|      AddRandomInt_UnorderedSet |  1000 | 18.143 μs | 0.2037 μs | 0.2985 μs | 18.054 μs | 11.7188 | 1.0986 |     - |   49216 B |
|      GetRandomInt_Dictionary__ |  1000 |  8.450 μs | 0.5649 μs | 0.7733 μs |  8.461 μs |       - |      - |     - |         - |
| GetRandomInt_UnorderedMapClass |  1000 |  7.767 μs | 0.4902 μs | 0.7337 μs |  7.763 μs |       - |      - |     - |         - |
|      GetRandomInt_UnorderedMap |  1000 |  8.966 μs | 0.5839 μs | 0.8186 μs |  8.427 μs |       - |      - |     - |         - |
|     GetRandomInt_UnorderedMap2 |  1000 |  9.653 μs | 0.5229 μs | 0.7499 μs |  9.286 μs |       - |      - |     - |         - |
