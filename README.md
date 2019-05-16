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
To get info about memory allocation add ```-m``` to the console arguments

To run tests for a single library
```shell
dotnet out/benchmarks.dll
```
to get console options



## Reports

Reports can be found in the BenchmarkDotNet.Artifacts/results folder.
[Report 4/1/19](BenchmarkDotNet.Artifacts/results/)

