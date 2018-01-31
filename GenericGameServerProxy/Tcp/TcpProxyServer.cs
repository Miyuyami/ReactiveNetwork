using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using GenericGameServerProxy.Abstractions;
using GenericGameServerProxy.Contracts;

namespace GenericGameServerProxy.Tcp
{
    public class TcpProxyServer : ProxyServer
    {
        private readonly TcpListener TcpListener;

        public virtual TimeSpan ClientReceiveTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public virtual TimeSpan ClientSendTimeout { get; set; } = TimeSpan.FromMinutes(1);

        public override IReadOnlyDictionary<Guid, IProxyClient> ConnectedClients => this.Clients;

        private readonly ConcurrentDictionary<Guid, IProxyClient> Clients = new ConcurrentDictionary<Guid, IProxyClient>();

        public TcpProxyServer(IPEndPoint proxyEndPoint, IPEndPoint targetEndPoint) : base(proxyEndPoint, targetEndPoint)
        {
            this.TcpListener = new TcpListener(this.ProxyEndPoint);
        }

        public TcpProxyServer(IPEndPoint proxyEndPoint, IPEndPoint targetEndPoint, string name) : base(proxyEndPoint, targetEndPoint, name)
        {
            this.TcpListener = new TcpListener(this.ProxyEndPoint);
        }

        private IObservable<IProxyClient> ClientStatusObservable;
        public override IObservable<IProxyClient> WhenClientStatusChanged() => this.ClientStatusObservable = this.ClientStatusObservable ??
            Observable.Create<IProxyClient>(ob =>
            {
                SerialDisposable sub2 = new SerialDisposable();
                var sub1 = this.WhenStatusChanged()
                               .Where(s => s == ProxyServerStatus.Started)
                               .Subscribe(__ =>
                               {
                                   sub2.Disposable = Observable.While(() => this.Status == ProxyServerStatus.Started,
                                                                      Observable.FromAsync(this.TcpListener.AcceptTcpClientAsync))
                                                               .Subscribe(tcpClient =>
                                                               {
                                                                   var client = new TcpProxyClient(tcpClient)
                                                                   {
                                                                       ReceiveTimeout = this.ClientReceiveTimeout,
                                                                       SendTimeout = this.ClientSendTimeout,
                                                                   };
                                                                   Guid guid;
                                                                   do
                                                                   {
                                                                       guid = Guid.NewGuid();
                                                                   } while (!this.Clients.TryAdd(guid, client));

                                                                   client.WhenStatusChanged()
                                                                         .Subscribe(___ => ob.OnNext(client));

                                                                   client.Start();

                                                                   // a TcpClient is not valid anymore after stopping
                                                                   client.WhenStatusChanged()
                                                                         .Where(s => s == ClientStatus.Stopped)
                                                                         .Subscribe(___ => this.Clients.TryRemove(guid, out _));
                                                               });
                               });

                return () =>
                {
                    sub1.Dispose();
                    sub2.Dispose();
                };
            })
            .Publish()
            .RefCount();

        protected override void InternalStart()
        {
            this.TcpListener.Start();
        }

        protected override void InternalStop()
        {
            this.TcpListener.Stop();

            foreach (var client in this.Clients.Values)
            {
                client.Stop();
            }

            this.Clients.Clear();
        }
    }
}
