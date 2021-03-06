﻿using System;
using System.Diagnostics;
using System.Threading;

using JetBrains.Annotations;

namespace Commons.TimeBasedUuid
{
    public class PreciseTimestampGenerator
    {
        public PreciseTimestampGenerator(TimeSpan syncPeriod, TimeSpan maxAllowedDivergence)
        {
            syncPeriodTicks = syncPeriod.Ticks;
            maxAllowedDivergenceTicks = maxAllowedDivergence.Ticks;
            baseTimestampTicks = DateTime.UtcNow.Ticks;
            lastTimestampTicks = baseTimestampTicks;
            stopwatchStartTicks = Stopwatch.GetTimestamp();
        }

        public long NowTicks()
        {
            var lastValue = Volatile.Read(ref lastTimestampTicks);
            while(true)
            {
                var nextValue = GenerateNextTimestamp(lastValue);
                var originalValue = Interlocked.CompareExchange(ref lastTimestampTicks, nextValue, lastValue);
                if(originalValue == lastValue)
                    return nextValue;
                lastValue = originalValue;
            }
        }

        // todo (andrew, 06.03.2017): consider using high precision Win API function GetSystemTimePreciseAsFileTime (https://msdn.microsoft.com/en-us/library/windows/desktop/hh706895.aspx)
        private long GenerateNextTimestamp(long localLastTimestampTicks)
        {
            var nowTicks = DateTime.UtcNow.Ticks;

            var localBaseTimestampTicks = Volatile.Read(ref baseTimestampTicks);
            var stopwatchElapsedTicks = Stopwatch.GetTimestamp() - stopwatchStartTicks;
            if(stopwatchElapsedTicks > syncPeriodTicks)
            {
                lock(this)
                {
                    baseTimestampTicks = localBaseTimestampTicks = nowTicks;
                    stopwatchStartTicks = Stopwatch.GetTimestamp();
                    stopwatchElapsedTicks = 0;
                }
            }

            var resultTicks = Math.Max(localBaseTimestampTicks + stopwatchElapsedTicks, localLastTimestampTicks + TicksPerMicrosecond);

            // see http://stackoverflow.com/questions/1008345
            if(stopwatchElapsedTicks < 0 || Math.Abs(resultTicks - nowTicks) > maxAllowedDivergenceTicks)
                return Math.Max(nowTicks, localLastTimestampTicks + TicksPerMicrosecond);

            return resultTicks;
        }

        public const long TicksPerMicrosecond = 10;

        [NotNull]
        public static readonly PreciseTimestampGenerator Instance = new PreciseTimestampGenerator(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(100));

        private readonly long syncPeriodTicks;
        private readonly long maxAllowedDivergenceTicks;
        private long baseTimestampTicks, lastTimestampTicks, stopwatchStartTicks;
    }
}