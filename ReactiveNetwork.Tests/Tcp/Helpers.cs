using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReactiveNetwork.Contracts;
using System.Net.Sockets;

namespace ReactiveNetwork.Tcp.Tests
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

        public static bool IsConnected(this TcpClient client)
        {
            try
            {
                if (client.Client?.Connected ?? false)
                {
                    // A = Poll; B = Receive; R = IsConnected
                    // A B R
                    // 0 0 1
                    // 0 1 1
                    // 1 0 0
                    // 1 1 1
                    //
                    // (~A + B)
                    // (!A || B)
                    return !client.Client.Poll(1 * 1000 * 1000, SelectMode.SelectRead) ||
                           (client.Client.Receive(new byte[1], SocketFlags.Peek) != 0);
                }
            }
            catch
            {

            }

            return false;
        }
    }
}