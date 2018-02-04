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
    public class TcpReactiveServer : ReactiveServer
    {
        private readonly TcpListener TcpListener;

        public virtual TimeSpan ClientReceiveTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public virtual TimeSpan ClientSendTimeout { get; set; } = TimeSpan.FromMinutes(1);

        public override IReadOnlyDictionary<Guid, IReactiveClient> ConnectedClients => this.Clients;

        private readonly ConcurrentDictionary<Guid, IReactiveClient> Clients = new ConcurrentDictionary<Guid, IReactiveClient>();

        public TcpReactiveServer(IPEndPoint endPoint) : base(endPoint)
        {
            this.TcpListener = new TcpListener(this.EndPoint);
        }

        public TcpReactiveServer(IPEndPoint endPoint, string name) : base(endPoint, name)
        {
            this.TcpListener = new TcpListener(this.EndPoint);
        }

        private IObservable<IReactiveClient> ClientStatusObservable;
        public override IObservable<IReactiveClient> WhenClientStatusChanged() => this.ClientStatusObservable = this.ClientStatusObservable ??
            Observable.Create<IReactiveClient>(ob =>
            {
                SerialDisposable sub2 = new SerialDisposable();
                var sub1 = this.WhenStatusChanged()
                               .Where(s => s == ServerStatus.Started)
                               .Subscribe(__ =>
                               {
                                   sub2.Disposable = Observable.While(() => this.Status == ServerStatus.Started,
                                                                      Observable.FromAsync(this.TcpListener.AcceptTcpClientAsync))
                                                               .Subscribe(tcpClient =>
                                                               {
                                                                   var client = new TcpReactiveClient(tcpClient)
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
