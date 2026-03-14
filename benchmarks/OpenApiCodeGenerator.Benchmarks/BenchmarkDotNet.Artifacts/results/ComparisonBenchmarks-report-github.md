```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7840/25H2/2025Update/HudsonValley2)
AMD Ryzen 7 7800X3D 4.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.200-preview.0.26103.119
  [Host]     : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v4
  DefaultJob : .NET 10.0.3 (10.0.3, 10.0.326.7603), X64 RyuJIT x86-64-v4


```
| Method   | Categories                     | Mean            | Error         | StdDev         | Median          | Ratio | RatioSD | Gen0        | Gen1      | Gen2    | Allocated      | Alloc Ratio |
|--------- |------------------------------- |----------------:|--------------:|---------------:|----------------:|------:|--------:|------------:|----------:|--------:|---------------:|------------:|
| Current  | GitHubApiNext,GenerateOnly     |    19,590.73 μs |    913.760 μs |   2,694.242 μs |    18,504.19 μs |     ? |       ? |    687.5000 |  343.7500 | 15.6250 |    37497.21 KB |           ? |
|          |                                |                 |               |                |                 |       |         |             |           |         |                |             |
| Released | GitHubApiNext,ParseAndGenerate | 4,275,059.67 μs | 82,573.361 μs | 113,027.358 μs | 4,264,655.55 μs |  1.00 |    0.04 | 204000.0000 | 9000.0000 |       - | 10055299.83 KB |        1.00 |
| Current  | GitHubApiNext,ParseAndGenerate |   758,891.62 μs | 14,788.118 μs |  23,023.320 μs |   754,015.15 μs |  0.18 |    0.01 |  13000.0000 | 8000.0000 |       - |   678324.84 KB |        0.07 |
|          |                                |                 |               |                |                 |       |         |             |           |         |                |             |
| Current  | GitHubApi,GenerateOnly         |    15,883.52 μs |    308.995 μs |     317.315 μs |    15,751.93 μs |     ? |       ? |    718.7500 |  343.7500 |       - |    39430.52 KB |           ? |
|          |                                |                 |               |                |                 |       |         |             |           |         |                |             |
| Released | GitHubApi,ParseAndGenerate     |   610,237.06 μs | 14,185.529 μs |  40,928.499 μs |   601,908.25 μs |  1.00 |    0.09 |   8000.0000 | 7000.0000 |       - |   435401.99 KB |        1.00 |
| Current  | GitHubApi,ParseAndGenerate     |   548,678.89 μs | 10,787.996 μs |  19,175.653 μs |   548,303.45 μs |  0.90 |    0.07 |   8000.0000 | 7000.0000 |       - |   412075.61 KB |        0.95 |
|          |                                |                 |               |                |                 |       |         |             |           |         |                |             |
| Current  | Petstore,GenerateOnly          |        18.11 μs |      0.732 μs |       2.053 μs |        17.33 μs |     ? |       ? |      1.1292 |    0.0305 |       - |       56.34 KB |           ? |
|          |                                |                 |               |                |                 |       |         |             |           |         |                |             |
| Released | Petstore,ParseAndGenerate      |       384.84 μs |      7.691 μs |       9.155 μs |       382.91 μs |  1.00 |    0.03 |     17.5781 |    7.8125 |       - |      883.69 KB |        1.00 |
| Current  | Petstore,ParseAndGenerate      |       474.75 μs |     26.998 μs |      79.604 μs |       454.69 μs |  1.23 |    0.21 |     16.6016 |    5.8594 |       - |      849.99 KB |        0.96 |
|          |                                |                 |               |                |                 |       |         |             |           |         |                |             |
| Current  | StripeApi,GenerateOnly         |    18,655.74 μs |    923.037 μs |   2,721.595 μs |    18,123.93 μs |     ? |       ? |    625.0000 |  250.0000 | 31.2500 |    34779.54 KB |           ? |
|          |                                |                 |               |                |                 |       |         |             |           |         |                |             |
| Released | StripeApi,ParseAndGenerate     |   419,754.60 μs | 15,256.846 μs |  44,745.693 μs |   417,412.80 μs |  1.01 |    0.15 |   5000.0000 | 4000.0000 |       - |   275602.76 KB |        1.00 |
| Current  | StripeApi,ParseAndGenerate     |   393,432.72 μs | 16,806.124 μs |  49,553.240 μs |   377,968.55 μs |  0.95 |    0.15 |   4000.0000 | 3000.0000 |       - |   255438.53 KB |        0.93 |
