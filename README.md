<div align="center">
  <img alt="ReDoc logo" src="https://raw.githubusercontent.com/catalyst-network/Community/master/media-pack/logo.png" width="400px" />

  ### Crypto Benchmarking

[![Discord](https://img.shields.io/discord/629667101774446593?color=blueviolet&label=discord)](https://discord.gg/anTP7xm)
![GitHub followers](https://img.shields.io/github/followers/catalyst-network?style=social)
[![GitHub stars](https://img.shields.io/github/stars/catalyst-network/community?style=social)](https://github.com/catalyst-network/protocol-protobuffs/stargazers)
[![Twitter Follow](https://img.shields.io/twitter/follow/catalystnetorg?style=social)](https://twitter.com/catalystnetorg)
[![Subreddit subscribers](https://img.shields.io/reddit/subreddit-subscribers/catalystnet?style=social)](https://reddit.com/r/catalystnet)
</div>

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
[Report 16/05/2019](BenchmarkDotNet.Artifacts/results/)

