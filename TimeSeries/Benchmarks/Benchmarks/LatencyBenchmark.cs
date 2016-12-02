﻿using System.ComponentModel;
using System.Linq;
using Benchmarks.ReadWrite;
using Benchmarks.Reflection;
using CassandraTimeSeries.Utils;

namespace Benchmarks.Benchmarks
{
    [BenchmarkClass]
    public class LatencyBenchmark : TimeSeriesBenchmark
    {
        [BenchmarkSetUp]
        public void SetUp()
        {
            var readerSettings = new ReaderSettings { MillisecondsSleep = 0 };
            var writerSettings = new WriterSettings { MillisecondsSleep = 0 };

            Readers = Enumerable.Range(0, 0).Select(_ => new BenchmarkEventReader(Series, readerSettings)).ToList();
            Writers = Enumerable.Range(0, 4).Select(_ => new BenchmarkEventWriter(Series, writerSettings)).ToList();

            Pool = new ReadersWritersPool(Readers, Writers);
        }
    }
}