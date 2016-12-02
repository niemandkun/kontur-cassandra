﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CassandraTimeSeries.Model;
using Commons;
using Metrics;

namespace Benchmarks.ReadWrite
{
    class BenchmarkEventReader : EventReader
    {
        public Timer LatencyTimer = Metric.Timer("Read Latency", Unit.Calls);
        public Counter EventsCounter = Metric.Counter("Events Read", Unit.Events);


        public TimeSpan AverageLatency => Latency.Average();
        public TimeSpan TotalTime => Latency.Sum();
        public List<TimeSpan> Latency { get; } = new List<TimeSpan>();
        public List<int> ReadsLength { get; } = new List<int>();
        public int TotalReadsCount => Latency.Count;
        public int TotalEventsRead => ReadsLength.Sum();

        public BenchmarkEventReader(TimeSeries series, ReaderSettings settings) 
            : base(series, settings) { }

        public override List<Event> ReadNext()
        {
            var sw = Stopwatch.StartNew();

            List<Event> events;
            
            using (LatencyTimer.NewContext())
                events = base.ReadNext();
            
            EventsCounter.Increment(events.Count);

            Latency.Add(sw.Elapsed);
            ReadsLength.Add(events.Count - 1);

            return events;
        }
    }
}
