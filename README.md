# crypto-benchmarking
Benchmarker for various crypto libraries

## Setup
build:
```shell
dotnet publish -c Release -o out
```

## Run benchmarks

To run all benchmarks and collate them into a single table:
```shell
out/benchmarks.dll -f '*' --join
```

To run single point of comparison eg benchmark the verification method of all libraries:
```shell
dotnet out/benchmarks.dll --anyCategories=verify —-join
```

To compare just ed25519 methods (or secp256k1)
```shell
dotnet out/benchmarks.dll --anyCategories=ed25519 --join
```
To get info about memory allocation add ```-m``` to the console arguments

To run tests for a single library
```shell
dotnet out/benchmarks.dll
```
to get console options



## Reports

Reports can be found in the BenchmarkDotNet.Artifacts/results folder.

[Report 4/1/19](BenchmarkDotNet.Artifacts/results/BenchmarkRun-joined-2019-01-04-01-35-42-report-github.md)

