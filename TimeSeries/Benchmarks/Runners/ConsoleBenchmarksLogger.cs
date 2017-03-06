﻿using System;
using Benchmarks.Benchmarks;

namespace Benchmarks.Runners
{
    class ConsoleBenchmarksLogger : BenchmarksLogger
    {
        public override void LogFixtureBegan(BenchmarksFixture fixture) => 
            Console.Write(CreateBigTitle($"Running fixture: {fixture.Name}"));

        public override void LogBenchmarkSetup(BenchmarksFixture fixture, Benchmark benchmark) => 
            Console.Write($"{NewLine}Setting up: {benchmark.Name} ... ");

        public override void LogBenchmarkStarted(BenchmarksFixture fixture, Benchmark benchmark) => 
            Console.Write($"Done!{NewLine}{NewLine}Running benchmark: {benchmark.Name} ... ");

        public override void LogBenchmarkFinished(BenchmarksFixture fixture, Benchmark benchmark, string result) => 
            Console.Write($"Done!{NewLine}{NewLine}{result}");

        public override void LogAllFixturesFinished()
        {
            Console.WriteLine($"{Separator}All benchmarks finished, press any key to exit.");
            Console.ReadKey();
        }
    }
}