using neXn.Pinger;
using NUnit.Framework;
using System;
using System.Net;

namespace PingerTests
{
    [TestFixture]
    public class Pinger
    {
        [SetUp]
        public void SetUp()
        {

        }
        [Test]
        public void AddressGenerationTest()
        {
            Ping p = new Ping(192, 168, new Ping.OctetRange() { From = 1, To = 15 }, new Ping.OctetRange() { From = 0, To = 255 });
            Assert.AreEqual(3840, p.Addresses.Length);

            p.RemoveAddress(IPAddress.Parse("192.168.1.0"));
            Assert.AreEqual(3839, p.Addresses.Length);

            p.RemoveAddresses(new IPAddress[] { IPAddress.Parse("192.168.1.0"), IPAddress.Parse("192.168.15.255") });
            Assert.AreEqual(3838, p.Addresses.Length);

            p.RemoveAddress(IPAddress.Parse("192.168.15.255"));
            Assert.AreEqual(3838, p.Addresses.Length);
        }
        [Test]
        public void RunPingTest()
        {
            Ping p = new Ping(192, 168, 10, new Ping.OctetRange() { From = 1, To = 20 });
            p.Run();

            Assert.AreEqual(20, p.PingReplies.Count);
        }
        [Test]
        public void RunPingAsyncTest()
        {
            bool isFinished = false;
            int reported = 0;
            Ping p = new Ping(192, 168, 10, new Ping.OctetRange() { From = 1, To = 20 });
            p.AsyncPingReport += ((o, e) => { Console.WriteLine($"Address ({e.PingReply.Address}) is {e.PingReply.IsAvailable}"); reported++; });
            p.AsyncPingFinished += ((o, e) => { isFinished = true; });
            p.RunAsync();

            while (!isFinished) { }

            Assert.AreEqual(20, reported);
        }

        [TearDown]
        public void TearDown()
        {

        }
    }
}