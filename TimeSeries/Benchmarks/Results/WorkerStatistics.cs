﻿using System;
using System.Collections.Generic;
using System.Linq;
using Benchmarks.ReadWrite;
using Commons;

namespace Benchmarks.Results
{
    abstract class WorkerStatistics
    {
        public TimeSpan AverageLatency { get; }
        public TimeSpan Latency95ThPercentile { get; }
        public TimeSpan Latency98ThPercentile { get; }

        public int TotalOperationsCount { get; }
        public double AverageOperationsPerThread { get; }
        public int Operations95ThPercentile { get; }
        public int Operations98ThPercentile { get; }
        public double TotalThroughput { get; }

        public WorkerStatistics(IReadOnlyList<IBenchmarkWorker> workers)
        {
            if (workers.Count == 0) return;

            AverageLatency = workers.SelectMany(x => x.Latency).Average();
            Latency95ThPercentile = workers.SelectMany(x => x.Latency).Percentile(95);
            Latency98ThPercentile = workers.SelectMany(x => x.Latency).Percentile(98);

            TotalOperationsCount = workers.Sum(x => x.TotalOperationsCount());
            AverageOperationsPerThread = workers.Select(x => x.TotalOperationsCount()).Average();
            Operations95ThPercentile = workers.Select(x => x.TotalOperationsCount()).Percentile(95);
            Operations98ThPercentile = workers.Select(x => x.TotalOperationsCount()).Percentile(98);
            TotalThroughput = workers.Select(x => x.AverageThroughput()).Sum();
        }
    }
}