﻿using Cassandra.Data.Linq;
using CassandraTimeSeries.Model;
using CassandraTimeSeries.Utils;
using Commons;
using Commons.TimeBasedUuid;

namespace CassandraTimeSeries.Series
{
    class CasSynchronizationHelper
    {
        private readonly TimeLinePartitioner partitioner;
        public long IdOfLastWrittenPartition { get; private set; }
        public TimeGuid LastWrittenTimeGuid { get; private set; }

        private readonly CasStartOfTimesHelper startOfTimesHelper;

        public long PartitionIdOfStartOfTimes => startOfTimesHelper.PartitionIdOfStartOfTimes;
        public TimeGuid StartOfTimes => startOfTimesHelper.StartOfTimes;

        public CasSynchronizationHelper(Table<CasTimeSeriesSyncData> synchronizationTable, TimeLinePartitioner partitioner)
        {
            this.partitioner = partitioner;
            startOfTimesHelper = new CasStartOfTimesHelper(synchronizationTable, partitioner);
        }

        public void UpdateIdOfLastWrittenPartition(long lastWrittenPartitionId)
        {
            IdOfLastWrittenPartition = lastWrittenPartitionId;
        }

        public void UpdateLastWrittenTimeGuid(StatementExecutionResult compareAndUpdateResult, EventsCollection eventToWrite)
        {
            if (compareAndUpdateResult.State == ExecutionState.PartitionClosed)
                LastWrittenTimeGuid = TimeGuid.MinForTimestamp(new Timestamp(eventToWrite.PartitionId) + partitioner.PartitionDuration);

            if (compareAndUpdateResult.State == ExecutionState.OutdatedId)
                LastWrittenTimeGuid = compareAndUpdateResult.PartitionMaxGuid;

            if (compareAndUpdateResult.State == ExecutionState.Success)
                LastWrittenTimeGuid = eventToWrite.LastEventId.ToTimeGuid();
        }

        public TimeGuid CreateSynchronizedId()
        {
            var nowGuid = TimeGuid.NowGuid();

            if (LastWrittenTimeGuid != null && LastWrittenTimeGuid.GetTimestamp() >= nowGuid.GetTimestamp())
                return LastWrittenTimeGuid.Increment();

            if (StartOfTimes.GetTimestamp() >= nowGuid.GetTimestamp())
                return StartOfTimes.Increment();

            return nowGuid;
        }
    }
}
