using System.Net;
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
        static TcpReactiveServer Server;

        [ClassInitialize]
        public static void InitilizeServer(TestContext testContext)
        {
            Server = new TcpReactiveServer(IPAddress.Parse("127.0.0.1"), 10101);
            Server.Start();
        }

        [TestMethod]
        public async Task TestCreateConnection()
        {
            var client = await TcpReactiveClient.CreateClientConnection(IPAddress.Parse("127.0.0.1"), 10000);
            Assert.IsNull(client);
            var client2 = await TcpReactiveClient.CreateClientConnection(IPAddress.Parse("127.0.0.1"), 10101);
            Assert.IsNotNull(client2);
            Assert.IsTrue(client2.IsConnected());
        }

        [TestMethod]
        public async Task TestWriteAndReceive()
        {
            byte[] bytes = new byte[] { 2, 3, 10, 8, 16 };
            var serverClientTask = Server.WhenClientStatusChanged().Where(c => c.Status == ClientStatus.Started).Take(1).ToTask();
            var client = await TcpReactiveClient.CreateClientConnection(IPAddress.Parse("127.0.0.1"), 10101);
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
    }
}
