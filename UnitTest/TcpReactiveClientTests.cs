using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReactiveNetwork.Contracts;
using ReactiveNetwork.Tcp;

namespace UnitTest
{
    [TestClass]
    public class TcpReactiveClientTests
    {
        private static readonly IPAddress IpAddress = IPAddress.Parse("127.0.0.1");
        private const int Port = 10101;
        private static readonly IPEndPoint EndPoint = new IPEndPoint(IpAddress, Port);

        private TcpReactiveServer Server;

        [TestInitialize]
        public void TestInitialize()
        {
            this.Server = new TcpReactiveServer(EndPoint);
            this.Server.Start();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.Server.Stop();
        }

        [TestMethod]
        public async Task TestCreateConnection()
        {
            var client = await TcpReactiveClient.CreateClientConnection(EndPoint);
            Assert.IsNotNull(client);
            await Assert.ThrowsExceptionAsync<SocketException>(() => TcpReactiveClient.CreateClientConnection(IpAddress, Port ^ 11111).ToTask());
            var client2 = await TcpReactiveClient.CreateClientConnection(EndPoint);
            Assert.IsNotNull(client2);
            client2.AssertIsConnected();
        }

        [TestMethod]
        public async Task TestWriteAndReceive()
        {
            byte[] bytes = new byte[] { 2, 3, 10, 8, 16 };
            var serverClientTask = this.Server.WhenClientStatusChanged().Where(c => c.Status == RunStatus.Started).Take(1).ToTask();
            var client = await TcpReactiveClient.CreateClientConnection(EndPoint);
            var serverClient = await serverClientTask;
            var receivedResultTask = serverClient.WhenDataReceived().Take(1).ToTask();
            var result = await client.Write(bytes);
            Assert.AreSame(client, result.Client);
            Assert.AreSame(bytes, result.Data);
            Assert.AreEqual(ClientEvent.Write, result.EventType);
            Assert.IsTrue(result.Success);
            var receivedResult = await receivedResultTask;
            Assert.AreSame(serverClient, receivedResult.Client);
            CollectionAssert.AreEqual(bytes, receivedResult.Data);
        }

        [TestMethod]
        public async Task TestClientReadTimeout()
        {
            var client = await TcpReactiveClient.CreateClientConnection(EndPoint, false);
            client.ReceiveTimeout = client.SendTimeout = TimeSpan.FromSeconds(6d);
            client.Start();

            client.AssertIsConnected();

            var t = client.WhenStatusChanged()
                          .Where(s => s == RunStatus.Stopped)
                          .ToTask();

            await Task.WhenAny(Task.Delay(client.ReceiveTimeout.Add(TimeSpan.FromSeconds(1d))), t);
            if (!t.IsCompleted)
            {
                Assert.Fail("should've completed");
            }

            client.AssertIsNotConnected();
        }

        [TestMethod]
        public async Task TestClientStopOnServerStop()
        {
            var client = await TcpReactiveClient.CreateClientConnection(EndPoint);
            var whenClientStoppedTask =
                client.WhenStatusChanged()
                      .Where(rs => rs == RunStatus.Stopped)
                      .ToTask();

            client.AssertIsConnected();

            await Task.Delay(1000);

            this.Server.Stop();

            await this.Server.WhenStatusChanged()
                             .Where(s => s == RunStatus.Stopped)
                             .Take(1);

            var t = client.WhenStatusChanged()
                          .Where(s => s == RunStatus.Stopped)
                          .ToTask();

            var delay = Task.Delay(TimeSpan.FromSeconds(client.RetryDelay.TotalSeconds * client.RetryCount).Add(TimeSpan.FromSeconds(1d)));
            await Task.WhenAny(delay, t);
            if (!t.IsCompleted)
            {
                Assert.Fail("should've completed");
            }

            client.AssertIsNotConnected();
        }

        [TestMethod]
        public async Task TestThat()
        {
            var source =
                this.Server.WhenClientStatusChanged()
                           .Where(c => c.Status == RunStatus.Started)
                           .SelectMany(c => c.WhenDataReceived())
                           .Where(cr => cr.Success);

            var sub =
                source.Subscribe(cr =>
                {
                    var data = cr.Data;

                    if (data.Length > 0)
                    {
                        switch (data[0])
                        {
                            case 0:
                                break;
                            case 1:
                                break;
                            case 2:
                                break;
                            case 3:
                                break;
                        }
                    }
                });
        }
    }
}
