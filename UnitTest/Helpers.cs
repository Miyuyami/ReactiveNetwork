using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReactiveNetwork.Contracts;
using ReactiveNetwork.Tcp;

namespace UnitTest
{
    public static class Helpers
    {
        public static void AssertIsNotConnected(this TcpReactiveClient client) =>
            Assert.IsTrue(
                client.Status == RunStatus.Stopped &&
                !client.IsConnected());

        public static void AssertIsConnected(this TcpReactiveClient client) =>
            Assert.IsTrue(
                client.Status == RunStatus.Started &&
                client.IsConnected());
    }
}
