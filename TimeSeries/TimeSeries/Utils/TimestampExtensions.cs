﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Commons;
using Commons.TimeBasedUuid;

namespace CassandraTimeSeries
{
    public static class TimestampExtensions
    {
        public static TimeGuid MinTimeGuid(this Timestamp timestamp)
        {
            return TimeGuid.MinForTimestamp(timestamp);
        }

        public static TimeGuid MaxTimeGuid(this Timestamp timestamp)
        {
            return TimeGuid.MaxForTimestamp(timestamp);
        }
    }
}
