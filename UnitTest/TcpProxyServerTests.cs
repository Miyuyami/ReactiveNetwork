using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using GenericGameServerProxy.Contracts;
using GenericGameServerProxy.Tcp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class TcpProxyServerTests
    {
        static IPAddress IPAddress = IPAddress.Parse("127.0.0.1");
        const int Port = 12345;
        static IPEndPoint ProxyEndPoint = new IPEndPoint(IPAddress, Port);
        static int SleepTime = 1000;

        [TestMethod]
        public void TestAllWithNoSleep()
        {
            int prevSleepTime = SleepTime;
            SleepTime = 0;

            try
            {
                //this.TestConnectBeforeSub();
                //this.TestConnectAfterSub();
                //this.TestSubBeforeStartConnectAfterSub();
                //this.TestMultipleConnect();
                //this.TestStop();
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

            var s = new TcpProxyServer(ProxyEndPoint, new IPEndPoint(IPAddress.Any, 0), "");
            try
            {
                s.Start();
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(ProxyEndPoint);
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

            var s = new TcpProxyServer(ProxyEndPoint, new IPEndPoint(IPAddress.Any, 0), "");
            try
            {
                s.Start();
                Thread.Sleep(SleepTime);
                var sub = s.WhenClientStatusChanged()
                           .Where(c => c.Status == ClientStatus.Started)
                           .Subscribe(c => count++);
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(ProxyEndPoint);
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

            var s = new TcpProxyServer(ProxyEndPoint, new IPEndPoint(IPAddress.Any, 0), "");
            try
            {
                var sub = s.WhenClientStatusChanged()
                           .Where(c => c.Status == ClientStatus.Started)
                           .Subscribe(c => count++);
                Thread.Sleep(SleepTime);
                s.Start();
                new TcpClient().Connect(ProxyEndPoint);
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

            var s = new TcpProxyServer(ProxyEndPoint, new IPEndPoint(IPAddress.Any, 0), "");
            try
            {
                s.Start();
                new TcpClient().Connect(ProxyEndPoint);
                new TcpClient().Connect(ProxyEndPoint);
                new TcpClient().Connect(ProxyEndPoint);
                var sub = s.WhenClientStatusChanged()
                           .Where(c => c.Status == ClientStatus.Started)
                           .Subscribe(c => count++);
                new TcpClient().Connect(ProxyEndPoint);
                new TcpClient().Connect(ProxyEndPoint);
                new TcpClient().Connect(ProxyEndPoint);
                new TcpClient().Connect(ProxyEndPoint);
                new TcpClient().Connect(ProxyEndPoint);
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(ProxyEndPoint);
                new TcpClient().Connect(ProxyEndPoint);
                new TcpClient().Connect(ProxyEndPoint);

                Thread.Sleep(1000);
                Assert.AreEqual(11, count);
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

            var s = new TcpProxyServer(ProxyEndPoint, new IPEndPoint(IPAddress.Any, 0), "");
            try
            {
                var sub = s.WhenClientStatusChanged()
                           .Where(c => c.Status == ClientStatus.Started)
                           .Subscribe(c => count++);
                Thread.Sleep(SleepTime);
                Assert.ThrowsException<SocketException>(() => new TcpClient().Connect(ProxyEndPoint));
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(ProxyEndPoint);
                Thread.Sleep(100);
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(ProxyEndPoint);
                Thread.Sleep(100);
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                Assert.ThrowsException<SocketException>(() => new TcpClient().Connect(ProxyEndPoint));
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                Assert.ThrowsException<SocketException>(() => new TcpClient().Connect(ProxyEndPoint));

                Thread.Sleep(1000);
                Assert.AreEqual(2, count);
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

            var s = new TcpProxyServer(ProxyEndPoint, new IPEndPoint(IPAddress.Any, 0), "");
            try
            {
                var sub = s.WhenClientStatusChanged()
                           .Where(c => c.Status == ClientStatus.Started)
                           .Subscribe(c => count++);
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(ProxyEndPoint);
                Thread.Sleep(100);
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(ProxyEndPoint);
                Thread.Sleep(100);
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                Assert.ThrowsException<SocketException>(() => new TcpClient().Connect(ProxyEndPoint));
                Thread.Sleep(100);
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                s.Stop();
                Thread.Sleep(SleepTime);
                s.Start();
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(ProxyEndPoint);
                Thread.Sleep(100);
                Thread.Sleep(SleepTime);
                new TcpClient().Connect(ProxyEndPoint);
                Thread.Sleep(100);

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

            var s = new TcpProxyServer(ProxyEndPoint, new IPEndPoint(IPAddress.Any, 0), "");
            try
            {
                s.WhenClientStatusChanged()
                 .Where(c => c.Status == ClientStatus.Started)
                 .Subscribe(c => count++);
                s.Start();

                new TcpClient().Connect(ProxyEndPoint);
                Thread.Sleep(100);
                new TcpClient().Connect(ProxyEndPoint);
                Thread.Sleep(100);
                new TcpClient().Connect(ProxyEndPoint);
                Thread.Sleep(100);
                new TcpClient().Connect(ProxyEndPoint);
                Thread.Sleep(100);

                Assert.AreEqual(count, s.Clients.Count);
                Assert.IsTrue(s.Clients.All(kvp => kvp.Value.Status == ClientStatus.Started));
            }
            finally
            {
                s.Stop();
            }
        }
    }
}
