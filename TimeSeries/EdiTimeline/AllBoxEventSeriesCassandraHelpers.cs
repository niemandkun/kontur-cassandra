using System;
using System.Globalization;
using Commons;
using JetBrains.Annotations;

namespace EdiTimeline
{
    public static class AllBoxEventSeriesCassandraHelpers
    {
        [NotNull]
        public static string FormatPartitionKey([NotNull] Timestamp eventTimestamp, TimeSpan partitionDuration)
        {
            return $"{eventTimestamp.Ticks - eventTimestamp.Ticks % partitionDuration.Ticks}";
        }

        [NotNull]
        public static Timestamp ParsePartitionKey([NotNull] string partitionKey)
        {
            return new Timestamp(long.Parse(partitionKey));
        }

        [NotNull]
        public static string NextPartitionKey([NotNull] string partitionKey, TimeSpan partitionDuration)
        {
            var partitionTicks = ParsePartitionKey(partitionKey).Ticks;
            return $"{partitionTicks + partitionDuration.Ticks}";
        }

        [NotNull]
        public static string FormatColumnName([NotNull] Timestamp eventTimestamp, Guid eventId)
        {
            return $"{eventTimestamp.Ticks.ToString("D20", CultureInfo.InvariantCulture)}_{eventId}";
        }

        [NotNull]
        public static AllBoxEventSeriesPointer ParseColumnName([NotNull] string columnName)
        {
            var parts = columnName.Split('_');
            var eventTimestamp = new Timestamp(long.Parse(parts[0]));
            var eventId = Guid.Parse(parts[1]);
            return new AllBoxEventSeriesPointer(eventTimestamp, eventId);
        }
    }
}