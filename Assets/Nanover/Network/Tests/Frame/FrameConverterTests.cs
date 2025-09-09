using System;
using Google.Protobuf.WellKnownTypes;
using Nanover.Core.Science;
using Nanover.Frame;
using Nanover.Grpc.Frame;
using Nanover.Protocol;
using Nanover.Protocol.Trajectory;
using NUnit.Framework;
using UnityEngine;

namespace Nanover.Grpc.Tests.Frame
{
    internal class FrameConverterTests
    {
        [Test]
        public void ReadMultipleBonds()
        {
            var data = new FrameData();
            data.SetBondPairs(new[] { 0u, 1u, 1u, 2u, 1u, 3u });

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(3, frame.Bonds.Count);

            Assert.AreEqual(new BondPair(0, 1), frame.Bonds[0]);
            Assert.AreEqual(new BondPair(1, 2), frame.Bonds[1]);
            Assert.AreEqual(new BondPair(1, 3), frame.Bonds[2]);
        }

        [Test]
        public void ReadZeroBonds()
        {
            var data = new FrameData();
            data.SetBondPairs(new uint[0]);

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(0, frame.Bonds.Count);
        }

        [Test]
        public void ReadMissingBonds()
        {
            var data = new FrameData();

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(0, frame.Bonds.Count);
        }

        [Test]
        public void ReadMultipleElements()
        {
            var data = new FrameData();
            data.SetParticleElements(new[] { 1u, 1u, 6u });

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(3, frame.ParticleElements.Length);

            Assert.AreEqual(Element.Hydrogen, frame.ParticleElements[0]);
            Assert.AreEqual(Element.Hydrogen, frame.ParticleElements[1]);
            Assert.AreEqual(Element.Carbon, frame.ParticleElements[2]);
        }

        [Test]
        public void ReadZeroParticles()
        {
            var data = new FrameData();

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(0, frame.Particles.Count);
        }

        [Test]
        public void ReadMultiplePositions()
        {
            var data = new FrameData();
            data.SetParticlePositions(new[] { 0f, 0f, 1f, 0f, 1f, 1f, 1f, -1f, 0f });

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(3, frame.ParticlePositions.Length);

            Assert.AreEqual(new Vector3(0f, 0f, 1f), frame.ParticlePositions[0]);
            Assert.AreEqual(new Vector3(0f, 1f, 1f), frame.ParticlePositions[1]);
            Assert.AreEqual(new Vector3(1f, -1f, 0f), frame.ParticlePositions[2]);
        }

        [Test]
        public void ReadOverlapPositions()
        {
            var data1 = new FrameData();
            data1.SetParticlePositions(new[] { 0f, 0f, 1f, 0f, 1f, 1f, 1f, -1f, 0f });

            var data2 = new FrameData();

            var data3 = new FrameData();
            data3.SetParticlePositions(new[] { 0f, 1f, 0f, 0f, 0f, 0f, 0f, 1f, 0f });

            var frame1 = FrameConverter.ConvertFrame(data1).Frame;
            var frame2 = FrameConverter.ConvertFrame(data2, frame1).Frame;
            var frame3 = FrameConverter.ConvertFrame(data3, frame2).Frame;

            Assert.AreEqual(3, frame1.Particles.Count);
            Assert.AreEqual(3, frame2.Particles.Count);
            Assert.AreEqual(3, frame3.Particles.Count);

            Assert.AreEqual(new Vector3(0f, 0f, 1f), frame1.ParticlePositions[0]);
            Assert.AreEqual(new Vector3(0f, 1f, 1f), frame1.ParticlePositions[1]);
            Assert.AreEqual(new Vector3(1f, -1f, 0f), frame1.ParticlePositions[2]);

            Assert.AreEqual(new Vector3(0f, 0f, 1f), frame2.ParticlePositions[0]);
            Assert.AreEqual(new Vector3(0f, 1f, 1f), frame2.ParticlePositions[1]);
            Assert.AreEqual(new Vector3(1f, -1f, 0f), frame2.ParticlePositions[2]);

            Assert.AreEqual(new Vector3(0f, 1f, 0f), frame3.ParticlePositions[0]);
            Assert.AreEqual(new Vector3(0f, 0f, 0f), frame3.ParticlePositions[1]);
            Assert.AreEqual(new Vector3(0f, 1f, 0f), frame3.ParticlePositions[2]);
        }

        [Test]
        public void ReadBonds_Update()
        {
            var data = new FrameData();
            data.SetBondPairs(new[] { 0u, 1u, 1u, 2u });

            var update = FrameConverter.ConvertFrame(data).Update;

            Assert.IsTrue(update.HasChanged(FrameData.BondArrayKey));
        }

        [Test]
        public void ReadElements_Update()
        {
            var data = new FrameData();
            data.SetParticleElements(new[] { 0u, 1u, 2u });

            var update = FrameConverter.ConvertFrame(data).Update;

            Assert.IsTrue(update.HasChanged(FrameData.ParticleElementArrayKey));
        }

        [Test]
        public void ReadPositions_Update()
        {
            var data = new FrameData();
            data.SetParticlePositions(new[] { 0f, 0f, 1f, 2f, 0.5f, 2f });

            var update = FrameConverter.ConvertFrame(data).Update;

            Assert.IsTrue(update.HasChanged(FrameData.ParticlePositionArrayKey));
        }

        [Test]
        public void ReadValue_Int()
        {
            var data = new FrameData();
            data.Values["id"] = Value.ForNumber(16.0);

            var (frame, update) = FrameConverter.ConvertFrame(data);

            Assert.AreEqual(16.0, frame.Data["id"]);
            Assert.IsTrue(update.HasChanged("id"));
        }

        [Test]
        public void ParticlePosition_WrongType()
        {
            var data = new FrameData()
            {
                {
                    FrameData.ParticlePositionArrayKey, new[] { "0", "1", "2", "0", "-1", "3" }
                }
            };

            Assert.Throws<ArgumentException>(() => FrameConverter.ConvertFrame(data));
        }

        [Test]
        public void ParticlePosition_WrongSize()
        {
            var data = new FrameData();
            data.SetParticlePositions(new[] { 0f, 1f, 2f, 3f, 4f });

            Assert.Throws<ArgumentException>(() => FrameConverter.ConvertFrame(data));
        }

        [Test]
        public void ParticleElement_WrongType()
        {
            var data = new FrameData()
            {
                {
                    FrameData.ParticleElementArrayKey, new[] { "0", "1", "2", "0", "-1", "3" }
                }
            };

            Assert.Throws<ArgumentException>(() => FrameConverter.ConvertFrame(data));
        }

        [Test]
        public void BondPairs_WrongType()
        {
            var data = new FrameData()
            {
                {
                    FrameData.BondArrayKey, new[] { "0", "1", "2", "0", "-1", "3" }
                }
            };

            Assert.Throws<ArgumentException>(() => FrameConverter.ConvertFrame(data));
        }

        [Test]
        public void BondPairs_WrongSize()
        {
            var data = new FrameData();
            data.SetBondPairs(new[] { 0u, 1u, 0u });

            Assert.Throws<ArgumentException>(() => FrameConverter.ConvertFrame(data));
        }

        [Test]
        public void Deserialize_FloatArray()
        {
            var data = new FrameData();
            data.AddFloatArray("id", new[] { 0f, 1f, 2f });

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(new[] { 0f, 1f, 2f }, frame.Data["id"]);
        }

        [Test]
        public void Deserialize_IndexArray()
        {
            var data = new FrameData();
            data.AddIndexArray("id", new[] { 0u, 1u, 2u });

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(new[] { 0u, 1u, 2u }, frame.Data["id"]);
        }

        [Test]
        public void Deserialize_StringArray()
        {
            var data = new FrameData();
            data.AddStringArray("id", new[] { "0", "1" });

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(new[] { "0", "1" }, frame.Data["id"]);
        }

        [Test]
        public void Deserialize_InvalidArray()
        {
            var data = new FrameData();
            data.Arrays["id"] = new ValueArray();

            Assert.Throws<ArgumentOutOfRangeException>(() => FrameConverter.ConvertFrame(data));
        }

        [Test]
        public void Deserialize_StringValue()
        {
            var data = new FrameData();
            data.Values["id"] = Value.ForString("test");

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual("test", frame.Data["id"]);
        }

        [Test]
        public void Deserialize_BoolValue()
        {
            var data = new FrameData();
            data.Values["id"] = Value.ForBool(true);

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(true, frame.Data["id"]);
        }

        [Test]
        public void Deserialize_NumberValue()
        {
            var data = new FrameData();
            data.Values["id"] = Value.ForNumber(2.0);

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(2.0, frame.Data["id"]);
        }

        [Test]
        public void Deserialize_NullValue()
        {
            var data = new FrameData();
            data.Values["id"] = Value.ForNull();

            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(null, frame.Data["id"]);
        }
        
        [Test]
        public void ReadCustomFloat()
        {
            var data = new FrameData();

            data.Values["test"] = Value.ForNumber(1.3f);
            
            var frame = FrameConverter.ConvertFrame(data).Frame;

            Assert.AreEqual(1.3f, frame.Data["test"]);
        }
    }
}