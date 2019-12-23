using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using MiscUtils.Logging;
using ReactiveNetwork.Abstractions;
using ReactiveNetwork.Contracts;

namespace ReactiveNetwork.Tcp
{
    public class TcpReactiveServer : ReactiveServer
    {
        private readonly TcpListener TcpListener;
        private readonly Socket Socket;

        public virtual TimeSpan ClientReceiveTimeout { get; set; } = TimeSpan.FromMinutes(1);
        public virtual TimeSpan ClientSendTimeout { get; set; } = TimeSpan.FromMinutes(1);

        public bool KeepAlive
        {
            get => (int)this.Socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive) != 0;
            set => this.SetKeepAlive(active: value);
        }

        private TimeSpan _KeepAliveInterval;
        public TimeSpan KeepAliveInterval
        {
            get => this._KeepAliveInterval;
            set
            {
                if (this._KeepAliveInterval != value)
                {
                    this._KeepAliveInterval = value;
                    this.SetKeepAlive(interval: value);
                }
            }
        }

        private TimeSpan _KeepAliveTime;
        public TimeSpan KeepAliveTime
        {
            get => this._KeepAliveTime;
            set
            {
                if (this._KeepAliveTime != value)
                {
                    this._KeepAliveTime = value;
                    this.SetKeepAlive(time: value);
                }
            }
        }

        public override IReadOnlyDictionary<Guid, IReactiveClient> ConnectedClients =>
            this.Clients;

        private readonly ConcurrentDictionary<Guid, IReactiveClient> Clients =
            new ConcurrentDictionary<Guid, IReactiveClient>();

        public TcpReactiveServer(IPAddress address, int port) : base(address, port)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            this.TcpListener = new TcpListener(address, port);
            this.Socket = this.TcpListener.Server;
        }

        public TcpReactiveServer(IPAddress address, int port, string name) : base(address, port, name)
        {
            if (address is null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            this.TcpListener = new TcpListener(address, port);
            this.Socket = this.TcpListener.Server;
        }

        public TcpReactiveServer(IPEndPoint endPoint) : base(endPoint)
        {
            if (endPoint is null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            this.TcpListener = new TcpListener(endPoint);
            this.Socket = this.TcpListener.Server;
        }

        public TcpReactiveServer(IPEndPoint endPoint, string name) : base(endPoint, name)
        {
            if (endPoint is null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            this.TcpListener = new TcpListener(endPoint);
            this.Socket = this.TcpListener.Server;
        }

        private IObservable<IReactiveClient> ClientStatusObservable;
        public override IObservable<IReactiveClient> WhenClientStatusChanged() => this.ClientStatusObservable ??=
            Observable.Create<IReactiveClient>(ob =>
            {
                var sub = this.WhenStatusChanged()
                              .Where(s => s == RunStatus.Started)
                              .Subscribe(__ =>
                              {
                                  Observable.While(() => this.Status == RunStatus.Started,
                                                   Observable.FromAsync(this.TcpListener.AcceptTcpClientAsync))
                                            .Subscribe(onNext: tcpClient =>
                                            {
                                                this.CreateClient(tcpClient)
                                                    .Subscribe(onNext: client =>
                                                    {
                                                        if (!this.Clients.TryAdd(client.Guid, client))
                                                        {
                                                            System.Diagnostics.Debug.Fail("this client already exists?? GUID collision?");
                                                            client.Stop();
                                                            return;
                                                        }

                                                        client.WhenStatusChanged()
                                                              .Subscribe(___ => ob.OnNext(client));

                                                        client.Start();

                                                        // a TcpClient is not valid anymore after stopping
                                                        client.WhenStatusChanged()
                                                              .Where(s => s == RunStatus.Stopped)
                                                              .Subscribe(___ => this.Clients.TryRemove(client.Guid, out _));
                                                    },
                                                    onError: e => SimpleLogger.Error(e));
                                            },
                                            onError: e => SimpleLogger.Error(e));
                              });

                return sub.Dispose;
                //return () =>
                //{
                //    sub.Dispose();
                //};
            })
            .Publish()
            .RefCount();

        protected virtual IObservable<IReactiveClient> CreateClient(TcpClient connectedTcpClient) =>
            Observable.Return(new TcpReactiveClient(Guid.NewGuid(), connectedTcpClient)
            {
                ReceiveTimeout = this.ClientReceiveTimeout,
                SendTimeout = this.ClientSendTimeout,
            });

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

        public void SetKeepAlive(bool? active = null, TimeSpan? interval = null, TimeSpan? time = null)
            => this.Socket.SetKeepAlive(active ?? this.KeepAlive,
                                        interval ?? this.KeepAliveInterval,
                                        time ?? this.KeepAliveTime);
    }
}
