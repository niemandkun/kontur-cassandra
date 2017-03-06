﻿using System;
using System.Diagnostics;
using System.Linq;
using Cassandra;
using Cassandra.Data.Linq;
using CassandraTimeSeries.Series;
using Commons;
using Commons.TimeBasedUuid;

namespace CassandraTimeSeries.Model
{
    public class CasTimeSeries : SimpleTimeSeries
    {
        private static readonly TimeUuid ClosingTimeUuid = TimeGuid.MaxValue.ToTimeUuid();

        private readonly CasSynchronizationHelper syncHelper;
        private readonly ISession session;

        public CasTimeSeries(Table<EventsCollection> eventsTable, Table<CasTimeSeriesSyncData> synchronizationTable, uint operationsTimeoutMilliseconds=10000) 
            : base(eventsTable, operationsTimeoutMilliseconds)
        {
            session = eventsTable.GetSession();
            syncHelper = new CasSynchronizationHelper(new CasStartOfTimesHelper(synchronizationTable));
        }

        public override Timestamp[] Write(params EventProto[] events)
        {
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < OperationsTimeoutMilliseconds)
            {
                var eventsToWrite = PackIntoCollection(events, syncHelper.CreateSynchronizedId);
                StatementExecutionResult statementExecutionResult;

                try
                {
                    statementExecutionResult = CompareAndUpdate(eventsToWrite);
                }
                catch (DriverException exception)
                {
                    Logger.Log(exception);
                    if (!ShouldRetryAfter(exception)) throw;
                    continue;
                }

                syncHelper.UpdateLastWrittenTimeGuid(statementExecutionResult, eventsToWrite);

                if (statementExecutionResult.State == ExecutionState.Success)
                    return eventsToWrite.Select(x => x.Timestamp).ToArray();
            }

            throw new OperationTimeoutException(OperationsTimeoutMilliseconds);
        }

        private StatementExecutionResult CompareAndUpdate(EventsCollection eventToWrite)
        {
            var isWritingToEmptyPartition = eventToWrite.PartitionId != syncHelper.IdOfLastWrittenPartition;
            syncHelper.UpdateIdOfLastWrittenPartition(eventToWrite.PartitionId);

            if (isWritingToEmptyPartition)
                CloseAllPartitionsBefore(syncHelper.IdOfLastWrittenPartition);

            return WriteEventToCurrentPartition(eventToWrite, isWritingToEmptyPartition);
        }

        private StatementExecutionResult WriteEventToCurrentPartition(EventsCollection e, bool isWritingToEmptyPartition)
        {
            return ExecuteStatement(CreateWriteEventStatement(e, isWritingToEmptyPartition));
        }

        private IStatement CreateWriteEventStatement(EventsCollection e, bool isWritingToEmptyPartition)
        {
            var writeEventStatement = session.Prepare(
                $"UPDATE {eventsTable.Name} " +
                "SET user_ids = ?, payloads = ?, max_id_in_partition = ?, event_ids = ? " +
                "WHERE last_event_id = ? AND partition_id = ? " +
                (isWritingToEmptyPartition ? "IF max_id_in_partition = NULL" : "IF max_id_in_partition < ?")
            );

            return isWritingToEmptyPartition
                ? writeEventStatement.Bind(e.UserIds, e.Payloads, e.LastEventId, e.EventIds, e.LastEventId, e.PartitionId)
                : writeEventStatement.Bind(e.UserIds, e.Payloads, e.LastEventId, e.EventIds, e.LastEventId, e.PartitionId, e.LastEventId);
        }

        private void CloseAllPartitionsBefore(long exclusiveLastPartitionId)
        {
            var idOfPartitionToClose = exclusiveLastPartitionId - Event.PartitionDutation.Ticks;
            var executionState = ExecutionState.Success;

            while (executionState != ExecutionState.PartitionClosed && idOfPartitionToClose >= syncHelper.PartitionIdOfStartOfTimes)
            {
                executionState = ExecuteStatement(CreateClosePartitionStatement(idOfPartitionToClose)).State;
                idOfPartitionToClose -= Event.PartitionDutation.Ticks;
            }
        }

        private IStatement CreateClosePartitionStatement(long idOfPartitionToClose)
        {
            return session.Prepare(
                $"UPDATE {eventsTable.Name} " +
                "SET max_id_in_partition = ? WHERE partition_id = ? " +
                "IF max_id_in_partition != ?"
            ).Bind(ClosingTimeUuid, idOfPartitionToClose, ClosingTimeUuid);
        }

        private StatementExecutionResult ExecuteStatement(IStatement statement)
        {
            var statementExecutionResult = session.Execute(statement).GetRows().Single();
            var isUpdateApplied = statementExecutionResult.GetValue<bool>("[applied]");

            if (isUpdateApplied) return new StatementExecutionResult {State = ExecutionState.Success};

            var partitionMaxTimeUuid = statementExecutionResult.GetValue<TimeUuid>("max_id_in_partition");

            return new StatementExecutionResult
            {
                State = partitionMaxTimeUuid == ClosingTimeUuid ? ExecutionState.PartitionClosed : ExecutionState.OutdatedId,
                PartitionMaxGuid = partitionMaxTimeUuid.ToTimeGuid()
            };
        }
    }
}