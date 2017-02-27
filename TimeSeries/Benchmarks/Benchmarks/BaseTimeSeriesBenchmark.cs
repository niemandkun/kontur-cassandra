using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Benchmarks.ReadWrite;
using Benchmarks.Reflection;
using Benchmarks.Results;
using CassandraTimeSeries.Interfaces;
using CassandraTimeSeries.Model;
using CassandraTimeSeries.ReadWrite;
using CassandraTimeSeries.Utils;
using Commons.TimeBasedUuid;

namespace Benchmarks.Benchmarks
{
    public abstract class BaseTimeSeriesBenchmark : BenchmarksFixture
    {
        protected abstract IDatabaseController Database { get; }
        protected abstract ITimeSeries TimeSeriesFactory();

        private ITimeSeries series;

        private readonly ReaderSettings readerSettings = new ReaderSettings();
        private readonly WriterSettings writerSettings = new WriterSettings();

        protected virtual int ReadersCount { get; } = 4;
        protected virtual int WritersCount { get; } = 4;
        protected virtual int PreloadedEventsCount { get; } = 5000;

        protected override void ClassSetUp()
        {
            Database.SetUpSchema();
            series = TimeSeriesFactory();
        }
        
        protected override void ClassTearDown()
        {
            Database.TearDownSchema();
        }

        protected override IEnumerable<Benchmark> GetBenchmarks()
        {
            return new[]
            {
                new Benchmark("Read and write", () => SetUp(ReadersCount, WritersCount), RunGenericBenchmark), 
                new Benchmark("Read only", () => {SetUp(ReadersCount, 0); FillEvents(); }, RunGenericBenchmark),
                new Benchmark("Write only", () => SetUp(0, WritersCount), RunGenericBenchmark), 
            };
        }

        private ReadersWritersPool<BenchmarkEventReader, BenchmarkEventWriter> pool;

        private void SetUp(int readersCount, int writersCount)
        {
            Database.ResetSchema();
            pool = InitReadersWritersPool(readersCount, writersCount);
        }

        private void FillEvents()
        {
            for (var i = 0; i < PreloadedEventsCount; ++i)
                series.WriteWithoutSync(new Event(TimeGuid.NowGuid(), new EventProto()));
        }

        private ReadersWritersPool<BenchmarkEventReader, BenchmarkEventWriter> InitReadersWritersPool(int readersCount, int writersCount)
        {
            var readers = Enumerable.Range(0, readersCount).Select(_ => new BenchmarkEventReader(TimeSeriesFactory(), readerSettings)).ToList();
            var writers = Enumerable.Range(0, writersCount).Select(_ => new BenchmarkEventWriter(TimeSeriesFactory(), writerSettings)).ToList();
            return new ReadersWritersPool<BenchmarkEventReader, BenchmarkEventWriter>(readers, writers);
        }

        private IBenchmarkingResult RunGenericBenchmark()
        {
            pool.Start();
            Thread.Sleep(5 * 1000);
            pool.Stop();

            return new DatabaseBenchmarkingResult(pool.Readers, pool.Writers);
        }
    }
}