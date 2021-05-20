using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReactiveNetwork.Contracts;

namespace ReactiveNetwork.Tcp.Tests
{
    [TestClass]
    public class TcpReactiveServerTests
    {
        private static readonly IPAddress IpAddress = IPAddress.Parse("127.0.0.1");
        private const int Port = 10102;
        private static readonly IPEndPoint EndPoint = new IPEndPoint(IpAddress, Port);

        private TcpReactiveServer Server;

        private ConcurrentBag<TcpClient> Clients;

        [TestInitialize]
        public void TestInitialize()
        {
            this.Server = new TcpReactiveServer(EndPoint);

            this.Clients = new ConcurrentBag<TcpClient>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.Server.Stop();

            while (this.Clients.Count > 0)
            {
                if (this.Clients.TryTake(out TcpClient c))
                {
                    c.Close();
                }
            }
        }

        public async Task<TcpClient> ConnectClientAsync(IPEndPoint endPoint = null)
        {
            if (endPoint is null)
            {
                endPoint = EndPoint;
            }

            var c = new TcpClient();
            this.Clients.Add(c);
            await c.ConnectAsync(endPoint.Address, endPoint.Port);
            return c;
        }

        [TestMethod]
        public async Task TestConnectBeforeSub()
        {
            const int take = 1;

            this.Server.Start();
            await this.ConnectClientAsync();

            var t = this.Server.WhenClientStatusChanged()
                               .Where(c => c.Status == RunStatus.Started)
                               .Take(take)
                               .Do(c => Console.WriteLine($"client started {c.Guid}"))
                               .ToTask();

            await Task.WhenAny(Task.Delay(2000), t);
            if (!t.IsCompleted)
            {
                Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
                Assert.Fail("should've completed");
            }

            Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
        }

        [TestMethod]
        public async Task TestConnectAfterSub()
        {
            const int take = 1;

            this.Server.Start();

            var t = this.Server.WhenClientStatusChanged()
                               .Where(c => c.Status == RunStatus.Started)
                               .Take(take)
                               .Do(c => Console.WriteLine($"client started {c.Guid}"))
                               .ToTask();

            await this.ConnectClientAsync();

            await Task.WhenAny(Task.Delay(2000), t);
            if (!t.IsCompleted)
            {
                Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
                Assert.Fail("should've completed");
            }

            Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
        }

        [TestMethod]
        public async Task TestSubBeforeStartConnectAfterSub()
        {
            const int take = 1;

            var t = this.Server.WhenClientStatusChanged()
                               .Where(c => c.Status == RunStatus.Started)
                               .Take(take)
                               .Do(c => Console.WriteLine($"client started {c.Guid}"))
                               .ToTask();

            this.Server.Start();
            await this.ConnectClientAsync();

            await Task.WhenAny(Task.Delay(2000), t);
            if (!t.IsCompleted)
            {
                Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
                Assert.Fail("should've completed");
            }

            Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
        }

        [TestMethod]
        public async Task TestMultipleConnect()
        {
            const int take = 8;

            var t = this.Server.WhenClientStatusChanged()
                               .Where(c => c.Status == RunStatus.Started)
                               .Take(take)
                               .Do(c => Console.WriteLine($"client started {c.Guid}"))
                               .ToTask();
            this.Server.Start();
            var tasks = new[]
            {
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
            };

            await Task.WhenAll(tasks);
            await Task.WhenAny(Task.Delay(20000), t);
            if (!t.IsCompleted)
            {
                Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
                Assert.Fail("should've completed");
            }

            Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
        }

        [TestMethod]
        public async Task TestStop()
        {
            const int take = 6;
            const int failed = 3;

            var t = this.Server.WhenClientStatusChanged()
                               .Where(c => c.Status == RunStatus.Started)
                               .Take(take)
                               .Do(c => Console.WriteLine($"client started {c.Guid}"))
                               .ToTask();
            await Assert.ThrowsExceptionAsync<SocketException>(() => this.ConnectClientAsync());
            this.Server.Start();
            var t2 = this.Server.WhenClientStatusChanged()
                                .Where(c => c.Status == RunStatus.Started)
                                .Take(4)
                                .ToTask();
            var tasks = new[]
            {
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
            };
            await Task.WhenAll(tasks);
            await Task.WhenAny(Task.Delay(10000), t2);
            if (!t2.IsCompleted)
            {
                Assert.Fail("should've completed");
            }
            this.Server.Stop();
            this.Server.Stop();
            this.Server.Start();
            var t3 = this.Server.WhenClientStatusChanged()
                                .Where(c => c.Status == RunStatus.Started)
                                .Take(1)
                                .ToTask();
            await this.ConnectClientAsync();
            await Task.WhenAny(Task.Delay(2000), t3);
            if (!t3.IsCompleted)
            {
                Assert.Fail("should've completed");
            }
            this.Server.Stop();
            await Assert.ThrowsExceptionAsync<SocketException>(() => this.ConnectClientAsync());
            this.Server.Start();
            this.Server.Stop();
            await Assert.ThrowsExceptionAsync<SocketException>(() => this.ConnectClientAsync());
            this.Server.Start();
            var t4 = this.Server.WhenClientStatusChanged()
                                .Where(c => c.Status == RunStatus.Started)
                                .Take(1)
                                .ToTask();
            await this.ConnectClientAsync();
            await Task.WhenAny(Task.Delay(2000), t4);
            if (!t4.IsCompleted)
            {
                Assert.Fail("should've completed");
            }
            this.Server.Stop();

            await Task.WhenAny(Task.Delay(1000), t);
            if (!t.IsCompleted)
            {
                Assert.AreEqual(this.Clients.Count, take + failed, "clients used doesn't equal to taken clients");
                Assert.Fail("should've completed");
            }

            Assert.AreEqual(this.Server.ConnectedClients.Count, 0);
            Assert.AreEqual(this.Clients.Count, take + failed, "clients used doesn't equal to taken clients");
        }

        [TestMethod]
        public async Task TestClientsAfterServerStop()
        {
            const int take = 5;

            var t = this.Server.WhenClientStatusChanged()
                               .Where(c => c.Status == RunStatus.Started)
                               .Take(take)
                               .Do(c => Console.WriteLine($"client started {c.Guid}"))
                               .ToTask();
            this.Server.Start();
            var t2 = this.Server.WhenClientStatusChanged()
                                .Where(c => c.Status == RunStatus.Started)
                                .Take(1)
                                .ToTask();
            await this.ConnectClientAsync();
            await Task.WhenAny(Task.Delay(2000), t2);
            if (!t2.IsCompleted)
            {
                Assert.Fail("should've completed");
            }
            this.Server.Stop();
            this.Server.Start();
            var t3 = this.Server.WhenClientStatusChanged()
                                .Where(c => c.Status == RunStatus.Started)
                                .Take(1)
                                .ToTask();
            await this.ConnectClientAsync();
            await Task.WhenAny(Task.Delay(2000), t3);
            if (!t3.IsCompleted)
            {
                Assert.Fail("should've completed");
            }
            this.Server.Stop();
            this.Server.Start();
            await this.ConnectClientAsync();
            await this.ConnectClientAsync();
            await this.ConnectClientAsync();

            await Task.WhenAny(Task.Delay(10000), t);
            if (!t.IsCompleted)
            {
                Assert.AreEqual(this.Clients.Count(c => c.IsConnected()), 3);
                Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
                Assert.Fail("should've completed");
            }

            Assert.AreEqual(this.Server.ConnectedClients.Count, 3);
            Assert.AreEqual(this.Clients.Count(c => c.IsConnected()), 3);
            Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
        }

        [TestMethod]
        public async Task TestCheckClientList()
        {
            const int take = 12;
            int count = 0;
            var timeout = TimeSpan.FromSeconds(5d);

            this.Server.ClientReceiveTimeout = timeout;
            var t = this.Server.WhenClientStatusChanged()
                               .Where(c => c.Status == RunStatus.Started)
                               .Take(take)
                               .Do(c => c.WhenStatusChanged()
                                         .Where(cs => cs == RunStatus.Stopped)
                                         .Subscribe(_ => count++))
                               .Do(c => Console.WriteLine($"client started {c.Guid}"))
                               .ToTask();
            this.Server.Start();
            Assert.AreEqual(this.Server.ConnectedClients.Count, 0, "client list should be empty at start");
            Assert.AreEqual(count, 0);

            var t2 = this.Server.WhenClientStatusChanged()
                                .Where(c => c.Status == RunStatus.Started)
                                .Take(4)
                                .ToTask();
            var tasks = new[]
            {
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
            };
            await Task.WhenAll(tasks);
            await Task.WhenAny(Task.Delay(tasks.Length * 2000), t2);
            if (!t2.IsCompleted)
            {
                Assert.Fail("should've completed");
            }
            Assert.AreEqual(this.Server.ConnectedClients.Count, tasks.Length);
            Assert.AreEqual(count, 0);

            this.Server.Stop();
            Assert.AreEqual(this.Server.ConnectedClients.Count, 0, "client list should be empty while stopped");
            Assert.AreEqual(count, 4);
            this.Server.Start();
            var t3 = this.Server.WhenClientStatusChanged()
                                .Where(c => c.Status == RunStatus.Started)
                                .Take(3)
                                .ToTask();
            var tasks2 = new[]
            {
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
            };
            await Task.WhenAll(tasks2);
            await Task.WhenAny(Task.Delay(tasks2.Length * 2000), t3);
            if (!t3.IsCompleted)
            {
                Assert.Fail("should've completed");
            }
            this.Server.ClientReceiveTimeout = TimeSpan.FromMinutes(5d);
            Assert.AreEqual(this.Server.ConnectedClients.Count, tasks2.Length);
            await Task.Delay(timeout.Add(TimeSpan.FromSeconds(2d)));
            Assert.AreEqual(this.Server.ConnectedClients.Count, 0, "all clients should've timed-out");

            var t4 = this.Server.WhenClientStatusChanged()
                                .Where(c => c.Status == RunStatus.Started)
                                .Take(5)
                                .ToTask();
            var tasks3 = new[]
            {
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
                this.ConnectClientAsync(),
            };
            await Task.WhenAll(tasks3);
            await Task.WhenAny(Task.Delay(tasks3.Length * 2000), t4);
            if (!t4.IsCompleted)
            {
                Assert.Fail("should've completed");
            }
            Assert.AreEqual(this.Server.ConnectedClients.Count, tasks3.Length);
            const int toClose = 2;
            var t5 = this.Server.WhenClientStatusChanged()
                                .Where(c => c.Status == RunStatus.Stopped)
                                .Take(toClose)
                                .ToTask();
            foreach (var c in this.Clients.Where(c => c.IsConnected())
                                          .Take(toClose))
            {
                c.Close();
                await Task.Delay(2000);
                Console.WriteLine("removing");
            }

            await Task.WhenAny(Task.Delay(toClose * 2000), t5);
            if (!t5.IsCompleted)
            {
                Assert.Fail("should've completed");
            }
            await Task.WhenAny(Task.Delay(toClose * 5000), t);
            if (!t.IsCompleted)
            {
                Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
                Assert.Fail("should've completed");
            }

            Assert.AreEqual(count, 9);
            Assert.AreEqual(this.Server.ConnectedClients.Count, tasks3.Length - toClose);
            foreach (var c in this.Server.ConnectedClients.Values)
            {
                ((TcpReactiveClient)c).AssertIsConnected();
            }

            Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
        }

        //[TestMethod]
        //public async Task TestCheckGuidConsistency()
        //{
        //    const int take = 1;

        //    var guids = new HashSet<Guid>();
        //    var sub = this.Server.WhenClientStatusChanged()
        //               .Where(c => c.Status == RunStatus.Started)
        //               .Subscribe(c => guids.Add(c.Guid));
        //    this.Server.Start();

        //    new TcpClient().Connect(EndPoint);
        //    new TcpClient().Connect(EndPoint);
        //    new TcpClient().Connect(EndPoint);

        //    Thread.Sleep(5000);
        //    Assert.AreEqual(3, this.Server.ConnectedClients.Count);
        //    Assert.AreEqual(this.Server.ConnectedClients.Count, guids.Count);

        //    Assert.IsTrue(guids.All(g => s.ConnectedClients.ContainsKey(g)));

        //    Assert.AreEqual(this.Clients.Count, take, "clients used doesn't equal to taken clients");
        //}
    }
}