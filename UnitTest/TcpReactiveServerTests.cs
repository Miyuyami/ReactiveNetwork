using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using ReactiveNetwork.Contracts;
using ReactiveNetwork.Tcp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class TcpReactiveServerTests
    {
        static IPAddress IPAddress = IPAddress.Parse("127.0.0.1");
        const int Port = 12345;
        static IPEndPoint EndPoint = new IPEndPoint(IPAddress, Port);
        static int SleepTime = 1000;

        [TestMethod]
        public void TestAllWithNoSleep()
        {
            int prevSleepTime = SleepTime;
            SleepTime = 0;

            try
            {
                this.TestConnectBeforeSub();
                this.TestConnectAfterSub();
                this.TestSubBeforeStartConnectAfterSub();
                this.TestMultipleConnect();
                this.TestStop();
                this.TestRestartConnect();
            }
            finally
            {
                SleepTime = prevSleepTime;
            }
        }

        [TestMethod]
        public void TestConnectBeforeSub()
        {
            int count = 0;

            var s = new TcpReactiveServer(EndPoint, "");
            try
            {
                s.Start();
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(EndPoint);
                Thread.Sleep(SleepTime);
                var sub = s.WhenClientStatusChanged()
                           .Where(c => c.Status == ClientStatus.Started)
                           .Subscribe(c => count++);
                Thread.Sleep(SleepTime);

                Thread.Sleep(1000);
                Assert.AreEqual(1, count);
            }
            finally
            {
                s.Stop();
            }
        }

        [TestMethod]
        public void TestConnectAfterSub()
        {
            int count = 0;

            var s = new TcpReactiveServer(EndPoint, "");
            try
            {
                s.Start();
                Thread.Sleep(SleepTime);
                var sub = s.WhenClientStatusChanged()
                           .Where(c => c.Status == ClientStatus.Started)
                           .Subscribe(c => count++);
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(EndPoint);
                Thread.Sleep(SleepTime);

                Thread.Sleep(1000);
                Assert.AreEqual(1, count);
            }
            finally
            {
                s.Stop();
            }
        }

        [TestMethod]
        public void TestSubBeforeStartConnectAfterSub()
        {
            int count = 0;

            var s = new TcpReactiveServer(EndPoint, "");
            try
            {
                var sub = s.WhenClientStatusChanged()
                           .Where(c => c.Status == ClientStatus.Started)
                           .Subscribe(c => count++);
                Thread.Sleep(SleepTime);
                s.Start();
                new TcpClient().Connect(EndPoint);
                Thread.Sleep(SleepTime);

                Thread.Sleep(1000);
                Assert.AreEqual(1, count);
            }
            finally
            {
                s.Stop();
            }
        }

        [TestMethod]
        public void TestMultipleConnect()
        {
            int count = 0;

            var s = new TcpReactiveServer(EndPoint, "");
            try
            {
                var sub = s.WhenClientStatusChanged()
                           .Where(c => c.Status == ClientStatus.Started)
                           .Subscribe(c => count++);
                s.Start();
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);

                Thread.Sleep(10000);
                Assert.AreEqual(8, count);
            }
            finally
            {
                s.Stop();
            }
        }

        [TestMethod]
        public void TestStop()
        {
            int count = 0;

            var s = new TcpReactiveServer(EndPoint, "");
            try
            {
                var sub = s.WhenClientStatusChanged()
                           .Where(c => c.Status == ClientStatus.Started)
                           .Subscribe(c => count++);
                Thread.Sleep(SleepTime);
                Assert.ThrowsException<SocketException>(() => new TcpClient().Connect(EndPoint));
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                Thread.Sleep(5000);
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(EndPoint);
                Thread.Sleep(2000);
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                Assert.ThrowsException<SocketException>(() => new TcpClient().Connect(EndPoint));
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                Assert.ThrowsException<SocketException>(() => new TcpClient().Connect(EndPoint));

                Thread.Sleep(2000);
                Assert.AreEqual(5, count);
            }
            finally
            {
                s.Stop();
            }
        }

        [TestMethod]
        public void TestRestartConnect()
        {
            int count = 0;

            var s = new TcpReactiveServer(EndPoint, "");
            try
            {
                var sub = s.WhenClientStatusChanged()
                           .Where(c => c.Status == ClientStatus.Started)
                           .Subscribe(c => count++);
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(EndPoint);
                Thread.Sleep(100);
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(EndPoint);
                Thread.Sleep(100);
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                Assert.ThrowsException<SocketException>(() => new TcpClient().Connect(EndPoint));
                Thread.Sleep(100);
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(EndPoint);
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(EndPoint);

                Thread.Sleep(1000);
                Assert.AreEqual(4, count);
            }
            finally
            {
                s.Stop();
            }
        }

        [TestMethod]
        public void TestCheckClientList()
        {
            int count = 0;

            var s = new TcpReactiveServer(EndPoint, "")
            {
                ClientReceiveTimeout = TimeSpan.FromSeconds(5),
            };
            try
            {
                s.WhenClientStatusChanged()
                 .Where(c => c.Status == ClientStatus.Started)
                 .Do(c => c.WhenStatusChanged()
                           .Where(cs => cs == ClientStatus.Stopped)
                           .Subscribe(_ => count--))
                 .Subscribe(_ => count++);
                s.Start();

                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                Thread.Sleep(10000);

                s.Stop();
                Assert.ThrowsException<SocketException>(() => new TcpClient().Connect(EndPoint));
                Assert.ThrowsException<SocketException>(() => new TcpClient().Connect(EndPoint));
                Thread.Sleep(100);
                s.Start();
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                new TcpClient().Connect(EndPoint);
                Thread.Sleep(10000);
                Assert.AreEqual(0, s.ConnectedClients.Count); // all timed-out

                var client1 = new TcpClient();
                var client2 = new TcpClient();
                var client3 = new TcpClient();
                client1.Connect(EndPoint);
                client2.Connect(EndPoint);
                client3.Connect(EndPoint);
                client3.Close();
                client1.Close();

                Thread.Sleep(2000);
                Assert.AreEqual(1, count);
                Assert.AreEqual(count, s.ConnectedClients.Count);
                Assert.IsTrue(s.ConnectedClients.All(kvp => kvp.Value.Status == ClientStatus.Started &&
                                                            ((TcpReactiveClient)kvp.Value).IsConnected()));
            }
            finally
            {
                s.Stop();
            }
        }
    }
}
