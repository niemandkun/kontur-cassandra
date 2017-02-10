﻿using System.Linq;
using Cassandra;
using Cassandra.Data.Linq;
using Commons;
using Commons.TimeBasedUuid;

namespace CassandraTimeSeries.Model
{
    public class CasTimeSeries : SimpleTimeSeries
    {
        private long lastSliceId;

        private readonly ISession session;

        public CasTimeSeries(Table<Event> table) : base(table)
        {
            session = table.GetSession();
        }

        public override Timestamp Write(EventProto ev)
        {
            Event eventToWrite;
            
            do
            {
                eventToWrite = new Event(TimeGuid.NowGuid(), ev);
            } while (!CompareAndUpdate(eventToWrite));

            return eventToWrite.Timestamp;
        }

        private bool CompareAndUpdate(Event eventToWrite)
        {
            var isFirstWrite = eventToWrite.SliceId != lastSliceId;
            lastSliceId = eventToWrite.SliceId;

            if (isFirstWrite)
                WriteMaxIdToPreviousSlice(eventToWrite);

            return WriteEventToCurrentSlice(eventToWrite, isFirstWrite);
        }

        private bool WriteEventToCurrentSlice(Event eventToWrite, bool isFirstWrite)
        {
            var updateStatement = session.Prepare(
                $"UPDATE {table.Name} " +
                "SET user_id = ?, payload = ?, max_id = ? " +
                "WHERE event_id = ? AND slice_id = ? " +
                (isFirstWrite ? "IF max_id = NULL" : "IF max_id < ?")
            );

            var e = eventToWrite;

            return Execute(isFirstWrite
                ? updateStatement.Bind(e.UserId, e.Payload, e.Id, e.Id, e.SliceId)
                : updateStatement.Bind(e.UserId, e.Payload, e.Id, e.Id, e.SliceId, e.Id));
        }

        private void WriteMaxIdToPreviousSlice(Event eventToWrite)
        {
            var prevSliceId = eventToWrite.SliceId - Event.SliceDutation.Ticks;

            var prevSliceUpdateStatement = session.Prepare(
                $"UPDATE {table.Name} " +
                "SET max_id = ? WHERE slice_id = ? " +
                "IF max_id < ?"
            ).Bind(eventToWrite.Id, prevSliceId, eventToWrite.Id);

            Execute(prevSliceUpdateStatement);
        }

        private bool Execute(IStatement statement)
        {
            return session.Execute(statement).GetRows().Select(x => x.GetValue<bool>("[applied]")).Single();
        }
    }
}