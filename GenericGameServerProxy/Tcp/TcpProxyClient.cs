using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using GenericGameServerProxy.Abstractions;

namespace GenericGameServerProxy.Tcp
{
    public class TcpProxyClient : ProxyClient
    {
        private readonly TcpClient TcpClient;

        public TcpProxyClient(TcpClient tcpClient) : base()
        {
            this.TcpClient = tcpClient;
        }

        protected override void InternalStart()
        {

        }

        protected override void InternalStop()
        {

        }

        public static IObservable<TcpProxyClient> CreateClientConnection(IPEndPoint targetIpEndPoint)
            => CreateClientConnection(targetIpEndPoint.Address, targetIpEndPoint.Port);

        public static IObservable<TcpProxyClient> CreateClientConnection(IPAddress ipAddress, int port) =>
            Observable.Create<TcpProxyClient>(ob =>
            {
                var tcpClient = new TcpClient();
                var sub = Observable.FromAsync(() => tcpClient.ConnectAsync(ipAddress, port))
                                    .Subscribe(onNext: _ => ob.Respond(new TcpProxyClient(tcpClient)),
                                               onError: _ => ob.Respond(null));

                return sub.Dispose;
                //return () =>
                //{
                //    sub.Dispose();
                //};
            });
    }
}
