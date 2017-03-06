﻿using CassandraTimeSeries.Model;
using CassandraTimeSeries.Utils;
using Commons;
using Commons.TimeBasedUuid;

namespace CassandraTimeSeries.Series
{
    class CasSynchronizationHelper
    {
        public long IdOfLastWrittenPartition { get; private set; }
        public TimeGuid TimeGuid { get; private set; }

        private readonly CasStartOfTimesHelper startOfTimesHelper;

        public long PartitionIdOfStartOfTimes => startOfTimesHelper.PartitionIdOfStartOfTimes;
        public TimeGuid StartOfTimes => startOfTimesHelper.StartOfTimes;

        public CasSynchronizationHelper(CasStartOfTimesHelper startOfTimesHelper)
        {
            this.startOfTimesHelper = startOfTimesHelper;
        }

        public void UpdateIdOfLastWrittenPartition(long lastWrittenPartitionId)
        {
            IdOfLastWrittenPartition = lastWrittenPartitionId;
        }

        public void UpdateLastWrittenTimeGuid(StatementExecutionResult compareAndUpdateResult, EventsCollection eventToWrite)
        {
            if (compareAndUpdateResult.State == ExecutionState.PartitionClosed)
                TimeGuid = TimeGuid.MinForTimestamp(new Timestamp(eventToWrite.PartitionId) + Event.PartitionDutation);

            if (compareAndUpdateResult.State == ExecutionState.OutdatedId)
                TimeGuid = compareAndUpdateResult.PartitionMaxGuid;

            if (compareAndUpdateResult.State == ExecutionState.Success)
                TimeGuid = eventToWrite.LastEventId.ToTimeGuid();
        }

        public TimeGuid CreateSynchronizedId()
        {
            var nowGuid = TimeGuid.NowGuid();

            if (TimeGuid != null && TimeGuid.GetTimestamp() >= nowGuid.GetTimestamp())
                return TimeGuid.Increment();

            if (StartOfTimes.GetTimestamp() >= nowGuid.GetTimestamp())
                return StartOfTimes.Increment();

            return nowGuid;
        }
    }
}
