using System;
using System.Collections;
using System.Collections.Generic;
using Google.Protobuf.WellKnownTypes;
using Nanover.Grpc;
using NUnit.Framework;

namespace Nanover.Utility.Protobuf.Tests
{
    public class ConversionTests
    {
        private static string[] testStrings = new[] {"", "a", "abc", "abc def", "abc\ndef"};
        private static bool[] testBools = new[] {true,false};
        private static double[] testNumbers = new[] {0.0, -1.0, 1.0, 4.2e3, -5.2e-2, Double.NaN};

        [Test]
        public void StringValue([ValueSource(nameof(testStrings))] string str)
        {
            var value = Value.ForString(str);
            Assert.AreEqual(str, value.ToObject());
        }
        
        [Test]
        public void BoolValue([ValueSource(nameof(testBools))] bool bl)
        {
            var value = Value.ForBool(bl);
            Assert.AreEqual(bl, value.ToObject());
        }
        
        [Test]
        public void NumberValue([ValueSource(nameof(testNumbers))] double num)
        {
            var value = Value.ForNumber(num);
            Assert.AreEqual(num, value.ToObject());
        }
        
        [Test]
        public void NullValue()
        {
            var value = Value.ForNull();
            Assert.AreEqual(null, value.ToObject());
        }
        
        [Test]
        public void EmptyList()
        {
            var value = Value.ForList();
            CollectionAssert.AreEqual(new object[0], value.ToObject() as IEnumerable);
        }
        
        [Test]
        public void MixedList()
        {
            var value = Value.ForList(Value.ForString("abc"), Value.ForNumber(1.0), Value.ForBool(false));
            CollectionAssert.AreEqual(new object[] {"abc", 1.0, false}, value.ToObject() as IEnumerable);
        }

        [Test]
        public void EmptyStruct()
        {
            var value = new Struct();
            CollectionAssert.AreEqual(new Dictionary<string, object>(), value.ToDictionary());
        }
    }
}
