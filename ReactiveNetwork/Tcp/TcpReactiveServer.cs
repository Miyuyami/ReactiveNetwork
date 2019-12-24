using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using ReactiveNetwork.Abstractions;
using ReactiveNetwork.Contracts;

namespace ReactiveNetwork.Tcp
{
    public class TcpReactiveServer : ReactiveServer
    {
        private readonly TcpListener TcpListener;
        private readonly Socket Socket;

        public virtual TimeSpan ClientReceiveTimeout { get; set; } = TimeSpan.FromMinutes(1d);
        public virtual TimeSpan ClientSendTimeout { get; set; } = TimeSpan.FromMinutes(1d);

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
                              .SelectMany(Observable.While(() => this.Status == RunStatus.Started,
                                                           Observable.FromAsync(this.TcpListener.AcceptTcpClientAsync)))
                              .SelectMany(this.CreateClient)
                              .Subscribe(client =>
                              {
                                  if (!this.ConnectedClients.TryAdd(client.Guid, client))
                                  {
                                      System.Diagnostics.Debug.Fail("client already exists? GUID collision?");
                                      client.Stop();
                                      return;
                                  }

                                  client.WhenStatusChanged()
                                        .Subscribe(_ => ob.OnNext(client));

                                  client.Start();

                                  // a client is not longer valid after stopping
                                  client.WhenStatusChanged()
                                        .Where(s => s == RunStatus.Stopped)
                                        .Subscribe(__ => this.ConnectedClients.TryRemove(client.Guid, out _));
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
            base.InternalStart();

            this.TcpListener.Start();
        }

        protected override void InternalStop()
        {
            this.TcpListener.Stop();

            base.InternalStop();
        }

        public void SetKeepAlive(bool? active = null, TimeSpan? interval = null, TimeSpan? time = null) =>
            this.Socket.SetKeepAlive(active ?? this.KeepAlive,
                                     interval ?? this.KeepAliveInterval,
                                     time ?? this.KeepAliveTime);
    }
}
