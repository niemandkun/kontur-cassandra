﻿using System;

namespace Benchmarks.Results
{
    public class BenchmarkingResult : IBenchmarkingResult
    {
        public TimeSpan AverageExecutionTime { get; }
        public TimeSpan TotalExecutionTime { get; }
        public IBenchmarkingResult AdditionalResult { get; }

        public BenchmarkingResult(TimeSpan executionTime, IBenchmarkingResult additionalResult = null)
        {
            TotalExecutionTime = AverageExecutionTime = executionTime;
            AdditionalResult = additionalResult;
        }

        private BenchmarkingResult(TimeSpan totalExecutionTime, TimeSpan averageExecutionTime, IBenchmarkingResult additionalResult=null)
        {
            TotalExecutionTime = totalExecutionTime;
            AverageExecutionTime = averageExecutionTime;
            AdditionalResult = additionalResult;
        }
        
        public string CreateReport()
        {
            var thisResult = $"Benchmark execution time: {TotalExecutionTime}\n\n";

            if (AdditionalResult != null)
                thisResult += AdditionalResult.CreateReport();

            return thisResult;
        }

        public IBenchmarkingResult Update(IBenchmarkingResult otherResult)
        {
            var other = otherResult as BenchmarkingResult;
            if (other == null)
                return this;

            var averageTime = TimeSpan.FromTicks((AverageExecutionTime.Ticks + other.AverageExecutionTime.Ticks)/2);
            var totalTime = TotalExecutionTime + other.TotalExecutionTime;
            return new BenchmarkingResult(totalTime, averageTime, other.AdditionalResult?.Update(AdditionalResult));
        }
    }
}
