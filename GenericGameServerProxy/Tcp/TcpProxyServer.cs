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

        public override IReadOnlyDictionary<Guid, IProxyClient> Clients => this._Clients;
        private ConcurrentDictionary<Guid, IProxyClient> _Clients { get; } = new ConcurrentDictionary<Guid, IProxyClient>();

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
                               .Subscribe(_ =>
                               {
                                   sub2.Disposable = Observable.While(() => this.Status == ProxyServerStatus.Started,
                                                                      Observable.FromAsync(this.TcpListener.AcceptTcpClientAsync))
                                                               .Subscribe(tcpClient =>
                                                               {
                                                                   var client = new TcpProxyClient(tcpClient);
                                                                   Guid guid;
                                                                   do
                                                                   {
                                                                       guid = Guid.NewGuid();
                                                                   } while (!this._Clients.TryAdd(guid, client));

                                                                   client.WhenStatusChanged()
                                                                         .Subscribe(__ => ob.OnNext(client));
                                                                   client.Start();
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
        }
    }
}
