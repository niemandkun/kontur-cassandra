﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GroBuf;
using GroBuf.DataMembersExtracters;
using NUnit.Framework;
using SKBKontur.Catalogue.Objects;
using SKBKontur.Catalogue.Objects.Bits;
using SKBKontur.Catalogue.Objects.TimeBasedUuid;

#pragma warning disable 1718

namespace CassandraTimeSeries.UnitTesting.Commons.TimeGuidTests
{
    [TestFixture]
    [SuppressMessage("ReSharper", "EqualExpressionComparison")]
    public class TimeGuidTest
    {
        [SetUp]
        public void SetUp()
        {
            var now = Timestamp.Now;
            g1 = TimeGuid.NewGuid(now);
            g2 = TimeGuid.NewGuid(now);
            g11 = new TimeGuid(g1.ToByteArray());
            g1PlusDelta = TimeGuid.NewGuid(now + TimeSpan.FromSeconds(1));
        }

        [Test]
        public void MinForTimestamp()
        {
            var timestamp = Timestamp.Now;
            var minGuidForTimestamp = TimeGuid.MinForTimestamp(timestamp);
            Assert.That(minGuidForTimestamp.GetTimestamp(), Is.EqualTo(timestamp));
            Assert.That(minGuidForTimestamp.GetClockSequence(), Is.EqualTo(TimeGuidBitsLayout.MinClockSequence));
            Assert.That(minGuidForTimestamp.GetNode(), Is.EqualTo(TimeGuidBitsLayout.MinNode));
        }

        [Test]
        public void MaxForTimestamp()
        {
            var timestamp = Timestamp.Now;
            var maxGuidForTimestamp = TimeGuid.MaxForTimestamp(timestamp);
            Assert.That(maxGuidForTimestamp.GetTimestamp(), Is.EqualTo(timestamp));
            Assert.That(maxGuidForTimestamp.GetClockSequence(), Is.EqualTo(TimeGuidBitsLayout.MaxClockSequence));
            Assert.That(maxGuidForTimestamp.GetNode(), Is.EqualTo(TimeGuidBitsLayout.MaxNode));
        }

        [Test]
        public void MinForTimestamp_ForGregorianCalendarStart()
        {
            Assert.That(TimeGuid.MinForTimestamp(TimeGuidBitsLayout.GregorianCalendarStart), Is.EqualTo(TimeGuid.MinValue));
        }

        [Test]
        public void MaxForTimestamp_ForGregorianCalendarEnd()
        {
            Assert.That(TimeGuid.MaxForTimestamp(TimeGuidBitsLayout.GregorianCalendarEnd), Is.EqualTo(TimeGuid.MaxValue));
        }

        [Test]
        public void MinMaxValues()
        {
            Assert.That(TimeGuid.MinValue.ToByteArray(), Is.EqualTo(TimeGuidBitsLayout.MinTimeGuid));
            Assert.That(TimeGuid.MinValue.ToGuid(), Is.EqualTo(new Guid("00000000-0000-1000-8080-808080808080")));
            Assert.That(TimeGuid.MaxValue.ToByteArray(), Is.EqualTo(TimeGuidBitsLayout.MaxTimeGuid));
            Assert.That(TimeGuid.MaxValue.ToGuid(), Is.EqualTo(new Guid("ffffffff-ffff-1fff-bf7f-7f7f7f7f7f7f")));
            Assert.That(TimeGuid.MaxValue, Is.GreaterThan(TimeGuid.MinValue));
        }

        [Test]
        public void NowGuid()
        {
            var nowDateTime = DateTime.UtcNow;
            var nowGuid = TimeGuid.NowGuid();
            Assert.That(nowGuid.GetTimestamp().Ticks - nowDateTime.Ticks, Is.LessThan(100 * 10000));
            Assert.That((nowGuid.GetTimestamp().ToDateTime() - nowDateTime).TotalMilliseconds, Is.LessThan(100));
        }

        [Test]
        [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
        public void InvalidV1Guid()
        {
            Assert.Throws<InvalidProgramStateException>(() => new TimeGuid(Guid.NewGuid()));
            Assert.Throws<InvalidProgramStateException>(() => new TimeGuid(Guid.Empty));
            Assert.Throws<InvalidProgramStateException>(() => new TimeGuid(GuidHelpers.MinGuid));
            Assert.Throws<InvalidProgramStateException>(() => new TimeGuid(GuidHelpers.MaxGuid));
            Assert.Throws<InvalidProgramStateException>(() => new TimeGuid(new byte[0]));
            Assert.Throws<InvalidProgramStateException>(() => new TimeGuid(TimeGuid.NowGuid().ToByteArray().Take(15).ToArray()));
            Assert.Throws<InvalidProgramStateException>(() => new TimeGuid(TimeGuid.NowGuid().ToByteArray().Concat(new byte[] {0xff}).ToArray()));
        }

        [Test]
        public void ToByteArray()
        {
            var timeGuid = TimeGuid.NowGuid();
            var bytes = timeGuid.ToByteArray();
            Assert.That(new TimeGuid(bytes), Is.EqualTo(timeGuid));
        }

        [Test]
        public void ToGuid()
        {
            var timeGuid = TimeGuid.NowGuid();
            Console.Out.WriteLine(timeGuid);
            var guid = timeGuid.ToGuid();
            Assert.That(new TimeGuid(guid), Is.EqualTo(timeGuid));
            Assert.That(new TimeGuid(guid).ToGuid(), Is.EqualTo(guid));
        }

        [Test]
        public void GetTimestamp()
        {
            var ts = Timestamp.Now;
            Assert.That(TimeGuid.NewGuid(ts).GetTimestamp(), Is.EqualTo(ts));
        }

        [Test]
        public void GetClockSequence()
        {
            var clockSequence = GetRandomClockSequence();
            Assert.That(TimeGuid.NewGuid(Timestamp.Now, clockSequence).GetClockSequence(), Is.EqualTo(clockSequence));
        }

        [Test]
        public void GetNode()
        {
            var node = rng.NextBytes(6);
            Assert.That(new TimeGuid(Timestamp.Now, GetRandomClockSequence(), node).GetNode(), Is.EqualTo(node));
        }

        [Test]
        public void Equals()
        {
            Assert.That(g1.Equals(null), Is.False);
            Assert.That(g1.Equals(g1), Is.True);
            Assert.That(g1.Equals(g11), Is.True);
            Assert.That(g1.Equals(g2), Is.False);
            Assert.That(g1.Equals(g1PlusDelta), Is.False);
        }

        [Test]
        public void EqualsOp()
        {
            Assert.That(g1 == null, Is.False);
            Assert.That(null == g1, Is.False);
            Assert.That(g1 == g1, Is.True);
            Assert.That(g1 == g11, Is.True);
            Assert.That(g1 == g2, Is.False);
            Assert.That(g1 == g1PlusDelta, Is.False);
        }

        [Test]
        public void NotEqualsOp()
        {
            Assert.That(g1 != null, Is.True);
            Assert.That(null != g1, Is.True);
            Assert.That(g1 != g1, Is.False);
            Assert.That(g1 != g11, Is.False);
            Assert.That(g1 != g2, Is.True);
            Assert.That(g1 != g1PlusDelta, Is.True);
        }

        [Test]
        public void GetHashCodeMethod()
        {
            Assert.That(g1.GetHashCode(), Is.EqualTo(ByteArrayComparer.Instance.GetHashCode(g1.ToByteArray())));
            Assert.That(g11.GetHashCode(), Is.EqualTo(g1.GetHashCode()));
            Assert.That(g2.GetHashCode(), Is.Not.EqualTo(g1.GetHashCode()));
        }

        [Test]
        public void CompareTo()
        {
            Assert.That(g1.CompareTo(null), Is.Positive);
            Assert.That(g1.CompareTo(g1), Is.EqualTo(0));
            Assert.That(g1.CompareTo(g11), Is.EqualTo(0));
            Assert.That(g11.CompareTo(g1), Is.EqualTo(0));
            Assert.That(g1.CompareTo(g1PlusDelta), Is.Negative);
            Assert.That(g1PlusDelta.CompareTo(g1), Is.Positive);
        }

        [Test]
        public void LessComparisonOps()
        {
            Assert.That(null < g1, Is.True);
            Assert.That(null <= g1, Is.True);
            Assert.That(g1 < null, Is.False);
            Assert.That(g1 <= null, Is.False);
            Assert.That(g1 < g1, Is.False);
            Assert.That(g1 <= g1, Is.True);
            Assert.That(g1 < g11, Is.False);
            Assert.That(g1 <= g11, Is.True);
            Assert.That(g1 < g1PlusDelta, Is.True);
            Assert.That(g1 <= g1PlusDelta, Is.True);
            Assert.That(g1PlusDelta < g1, Is.False);
            Assert.That(g1PlusDelta < g1, Is.False);
        }

        [Test]
        public void GreaterComparisonOps()
        {
            Assert.That(null > g1, Is.False);
            Assert.That(null >= g1, Is.False);
            Assert.That(g1 > null, Is.True);
            Assert.That(g1 >= null, Is.True);
            Assert.That(g1 > g1, Is.False);
            Assert.That(g1 >= g1, Is.True);
            Assert.That(g1 > g11, Is.False);
            Assert.That(g1 >= g11, Is.True);
            Assert.That(g1 > g1PlusDelta, Is.False);
            Assert.That(g1 >= g1PlusDelta, Is.False);
            Assert.That(g1PlusDelta > g1, Is.True);
            Assert.That(g1PlusDelta > g1, Is.True);
        }

        [Test]
        public void CompareTo_ByTimestamp()
        {
            var timestamp1 = Timestamp.Now;
            var timestamp2 = timestamp1.AddTicks(1);
            var clockSequence = GetRandomClockSequence();
            var timeGuid1 = new TimeGuid(timestamp1, clockSequence, new byte[6]);
            var timeGuid2 = new TimeGuid(timestamp2, clockSequence, new byte[6]);
            Assert.That(timeGuid2.CompareTo(timeGuid1), Is.Positive);
            Assert.That(timeGuid1.CompareTo(timeGuid2), Is.Negative);
        }

        [Test]
        [Category("Nightly")]
        public void CompareTo_ByTimestamp_SequentialTimestamps()
        {
            TimeGuid lastGuid = null;
            foreach(var timestamp in TimeGuidBitsLayoutTest.AllDistinctTimestamps(TimeSpan.FromHours(10)))
            {
                var nextwGuid = TimeGuid.NewGuid(timestamp);
                Assert.That(lastGuid < nextwGuid);
                Assert.That(nextwGuid > lastGuid);
                if(lastGuid != null)
                {
                    Assert.That(nextwGuid, Is.GreaterThan(lastGuid));
                    Assert.That(nextwGuid.GetTimestamp(), Is.GreaterThan(lastGuid.GetTimestamp()));
                }
                lastGuid = nextwGuid;
            }
        }

        [Test]
        [Category("Nightly")]
        public void CompareTo_ByTimestamp_AllDistinctTimestamps()
        {
            foreach(var timestamp1 in TimeGuidBitsLayoutTest.AllDistinctTimestamps(TimeSpan.FromDays(10000)))
            {
                foreach(var timestamp2 in TimeGuidBitsLayoutTest.AllDistinctTimestamps(TimeSpan.FromDays(10)))
                {
                    if(timestamp2 == timestamp1)
                        continue;
                    var timeGuid1 = TimeGuid.NewGuid(timestamp1);
                    var timeGuid2 = TimeGuid.NewGuid(timestamp2);
                    var expectedResult = timestamp1.CompareTo(timestamp2);
                    Assert.That(timeGuid1.CompareTo(timeGuid2), Is.EqualTo(expectedResult), string.Format("timestamp1 = {0}, timestamp2 = {1}", timestamp1, timestamp2));
                }
            }
        }

        [Test]
        public void CompareTo_ByClockSequence()
        {
            var timestamp = Timestamp.Now;
            ushort clockSequence1 = 0;
            ushort clockSequence2 = 1;
            while(clockSequence2 <= TimeGuidBitsLayout.MaxClockSequence)
            {
                var timeGuid1 = new TimeGuid(timestamp, clockSequence1, new byte[6]);
                var timeGuid2 = new TimeGuid(timestamp, clockSequence2, new byte[6]);
                Assert.That(timeGuid1.CompareTo(timeGuid1), Is.EqualTo(0));
                Assert.That(timeGuid2.CompareTo(timeGuid2), Is.EqualTo(0));
                Assert.That(timeGuid1.CompareTo(timeGuid2), Is.Negative);
                Assert.That(timeGuid2.CompareTo(timeGuid1), Is.Positive);
                clockSequence1++;
                clockSequence2++;
            }
        }

        [Test]
        [Category("Nightly")]
        public void CompareTo_ByClockSequence_AllDistinctValues()
        {
            var node = new byte[6];
            var timestamp = Timestamp.Now;
            const int step = 10;
            for(var clockSequence1 = TimeGuidBitsLayout.MinClockSequence; clockSequence1 <= TimeGuidBitsLayout.MaxClockSequence; clockSequence1++)
            {
                for(var clockSequence2Base = TimeGuidBitsLayout.MinClockSequence; clockSequence2Base < TimeGuidBitsLayout.MaxClockSequence - step; clockSequence2Base += step)
                {
                    var clockSequence2 = (ushort)(clockSequence2Base + rng.Next(step));
                    var timeGuid1 = new TimeGuid(timestamp, clockSequence1, node);
                    var timeGuid2 = new TimeGuid(timestamp, clockSequence2, node);
                    var expectedResult = clockSequence1.CompareTo(clockSequence2);
                    Assert.That(timeGuid1.CompareTo(timeGuid2), Is.EqualTo(expectedResult), string.Format("clockSequence1 = {0}, clockSequence2 = {1}", clockSequence1, clockSequence2));
                }
            }
        }

        [Test]
        public void CompareTo_ByNode()
        {
            var timestamp = Timestamp.Now;
            var clockSequence = GetRandomClockSequence();
            var timeGuid1 = new TimeGuid(timestamp, clockSequence, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00});
            var timeGuid2 = new TimeGuid(timestamp, clockSequence, new byte[] {0x01, 0x00, 0x00, 0x00, 0x00, 0x00});
            var timeGuid3 = new TimeGuid(timestamp, clockSequence, new byte[] {0x12, 0x34, 0x56, 0x78, 0xfe, 0xff});
            var timeGuid4 = new TimeGuid(timestamp, clockSequence, new byte[] {0x12, 0x34, 0x56, 0x78, 0xff, 0x00});
            var timeGuid5 = new TimeGuid(timestamp, clockSequence, new byte[] {0x12, 0x34, 0x56, 0x78, 0xff, 0xff});
            var timeGuid6 = new TimeGuid(timestamp, clockSequence, new byte[] {0x12, 0x34, 0x56, 0x79, 0x00, 0x00});
            var timeGuid7 = new TimeGuid(timestamp, clockSequence, new byte[] {0x7f, 0x00, 0x00, 0x00, 0x00, 0x00});
            var timeGuid8 = new TimeGuid(timestamp, clockSequence, new byte[] {0x7f, 0x01, 0x00, 0x00, 0x00, 0x00});
            var timeGuid9 = new TimeGuid(timestamp, clockSequence, new byte[] {0xff, 0x00, 0x00, 0x00, 0x00, 0x00});
            var timeGuid10 = new TimeGuid(timestamp, clockSequence, new byte[] {0xff, 0x00, 0x12, 0x34, 0x00, 0x00});
            var timeGuid11 = new TimeGuid(timestamp, clockSequence, new byte[] {0xff, 0x00, 0x12, 0x35, 0x00, 0x00});
            var timeGuid12 = new TimeGuid(timestamp, clockSequence, new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0x01});
            var timeGuid13 = new TimeGuid(timestamp, clockSequence, new byte[] {0xff, 0xff, 0xff, 0xff, 0xff, 0xff});
            Assert.That(timeGuid1.CompareTo(timeGuid2), Is.Negative);
            Assert.That(timeGuid2.CompareTo(timeGuid3), Is.Negative);
            Assert.That(timeGuid3.CompareTo(timeGuid4), Is.Negative);
            Assert.That(timeGuid4.CompareTo(timeGuid5), Is.Negative);
            Assert.That(timeGuid5.CompareTo(timeGuid6), Is.Negative);
            Assert.That(timeGuid6.CompareTo(timeGuid7), Is.Negative);
            Assert.That(timeGuid7.CompareTo(timeGuid8), Is.Negative);
            Assert.That(timeGuid8.CompareTo(timeGuid9), Is.Negative);
            Assert.That(timeGuid9.CompareTo(timeGuid10), Is.Negative);
            Assert.That(timeGuid10.CompareTo(timeGuid11), Is.Negative);
            Assert.That(timeGuid11.CompareTo(timeGuid12), Is.Negative);
            Assert.That(timeGuid12.CompareTo(timeGuid13), Is.Negative);
        }

        [Test]
        [Category("Nightly")]
        public void StrongOrdering_ByTimestampNow()
        {
            var lastGuid = TimeGuid.MinValue;
            for(var i = 0; i < 5 * 1000 * 1000; i++)
            {
                var nowGuid = TimeGuid.NowGuid();
                Assert.That(nowGuid.GetTimestamp(), Is.GreaterThan(lastGuid.GetTimestamp()));
                lastGuid = nowGuid;
            }
        }

        [Test]
        public void TryParse()
        {
            TimeGuid actual;
            Assert.That(TimeGuid.TryParse(null, out actual), Is.False);
            Assert.That(actual, Is.Null);

            Assert.That(TimeGuid.TryParse("some-string", out actual), Is.False);
            Assert.That(actual, Is.Null);

            Assert.That(TimeGuid.TryParse(Guid.NewGuid().ToString(), out actual), Is.False);
            Assert.That(actual, Is.Null);

            var timeGuid = TimeGuid.NowGuid();
            Assert.That(TimeGuid.TryParse(timeGuid.ToGuid().ToString(), out actual), Is.True);
            Assert.That(actual, Is.EqualTo(timeGuid));
        }

        [Test]
        public void Serialization_Grobuf()
        {
            var timeGuid = TimeGuid.NowGuid();
            var mergeOnReadSerializer = new Serializer(new AllPropertiesExtractor(), new DefaultGroBufCustomSerializerCollection(), GroBufOptions.MergeOnRead);
            Func<TestDto, TestDto> serializeAndDeserialize = dto =>
                {
                    var bytes = mergeOnReadSerializer.Serialize(dto);
                    return mergeOnReadSerializer.Deserialize<TestDto>(bytes);
                };
            Assert.That(serializeAndDeserialize(new TestDto {TimeGuid = timeGuid}).TimeGuid, Is.EqualTo(timeGuid));
            Assert.That(serializeAndDeserialize(new TestDto {TimeGuid = TimeGuid.MinValue}).TimeGuid, Is.EqualTo(TimeGuid.MinValue));
            Assert.That(serializeAndDeserialize(new TestDto {TimeGuid = TimeGuid.MaxValue}).TimeGuid, Is.EqualTo(TimeGuid.MaxValue));
            Assert.That(serializeAndDeserialize(new TestDto {TimeGuid = null}).TimeGuid, Is.Null);
        }

        [Test]
        [Category("Manual")]
        public void Perf()
        {
            const int count = 10 * 1000 * 1000;
            var sw = Stopwatch.StartNew();
            for(var i = 0; i < count; i++)
                TimeGuid.NowGuid();
            sw.Stop();
            Console.Out.WriteLine("TimeGuid.NowGuid() took {0} ms for {1} calls", sw.ElapsedMilliseconds, count);
        }

        private ushort GetRandomClockSequence()
        {
            return rng.NextUshort(TimeGuidBitsLayout.MinClockSequence, TimeGuidBitsLayout.MaxClockSequence + 1);
        }

        private TimeGuid g1;
        private TimeGuid g2;
        private TimeGuid g11;
        private TimeGuid g1PlusDelta;
        private readonly Random rng = new Random();

        private class TestDto
        {
            public TimeGuid TimeGuid { get; set; }
        }
    }
}