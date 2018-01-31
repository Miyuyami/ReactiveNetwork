using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using GenericGameServerProxy.Abstractions;
using GenericGameServerProxy.Contracts;

namespace GenericGameServerProxy.Tcp
{
    public class TcpProxyClient : ProxyClient
    {
        public TcpClient TcpClient { get; }
        public Socket Socket { get; }
        public NetworkStream NetworkStream { get; private set; }

        public virtual int RetryCount { get; set; } = 5;
        public virtual TimeSpan ReceiveTimeout { get; set; }
        public virtual TimeSpan SendTimeout { get; set; }

        private const int _BufferLength = 1 << 20; // 1MiB
        private readonly byte[] _Buffer = new byte[_BufferLength];

        protected internal TcpProxyClient(TcpClient connectedTcpClient) : base()
        {
            this.TcpClient = connectedTcpClient;
            this.Socket = this.TcpClient.Client;
            this.NetworkStream = this.TcpClient.GetStream();
        }

        private IObservable<int> NetworkStreamReadObservable(IObserver<ClientResult> ob) =>
            Observable.FromAsync(t => this.NetworkStream.ReadAsync(this._Buffer, 0, _BufferLength, t))
                      .Timeout(this.ReceiveTimeout)
                      .Catch<int, ObjectDisposedException>(_ =>
                      {
                          ob.OnCompleted();
                          return Observable.Return(-1);
                      })
                      .Catch<int, TimeoutException>(_ =>
                      {
                          ob.OnCompleted();
                          return Observable.Return(-1);
                      })
                      .Retry(this.RetryCount);

        private IConnectableObservable<ClientResult> DataReceivedConnectableObservable;
        private IDisposable DataReceivedConnectionDisposable;

        private void InitDataReceived()
        {
            this.DataReceivedConnectableObservable =
               Observable.Create<ClientResult>(ob =>
               {
                   SerialDisposable sub2 = new SerialDisposable();
                   var sub1 = this.WhenStatusChanged()
                                  .Where(s => s == ClientStatus.Started)
                                  .Take(1) // ensure sub is only hit once
                                  .Subscribe(_ =>
                                  {
                                      sub2.Disposable = Observable.While(() => this.IsConnected(), this.NetworkStreamReadObservable(ob))
                                                                  .Subscribe(
                                                                  onNext: receivedBytes =>
                                                                  {
                                                                      if (receivedBytes > 0)
                                                                      {
                                                                          byte[] readBytes = new byte[receivedBytes];
                                                                          Buffer.BlockCopy(this._Buffer, 0, readBytes, 0, receivedBytes);
                                                                          ob.OnNext(new ClientResult(this, ClientEvent.Read, readBytes));
                                                                      }
                                                                  },
                                                                  onCompleted: ob.OnCompleted);
                                  });

                   return () =>
                   {
                       sub1.Dispose();
                       sub2.Dispose();
                   };
               })
               .Finally(() => this.Stop())
               .Publish();

            this.DataReceivedConnectionDisposable = this.DataReceivedConnectableObservable.Connect();
        }

        private IObservable<ClientResult> DataReceivedObservable;
        public override IObservable<ClientResult> WhenDataReceived() => this.DataReceivedObservable = this.DataReceivedObservable ??
            this.DataReceivedConnectableObservable.RefCount();

        public override IObservable<ClientResult> Read() =>
            this.WhenDataReceived()
                .Take(1);

        // TODO: may throw TimeoutException
        public override IObservable<ClientResult> Write(byte[] bytes) =>
            Observable.FromAsync(t => this.NetworkStream.WriteAsync(bytes, 0, bytes.Length, t))
            .Timeout(this.SendTimeout)
            .Select(_ => new ClientResult(this, ClientEvent.Write, bytes));


        public override void WriteWithoutReponse(byte[] bytes) =>
            this.Write(bytes);

        public bool IsConnected()
        {
            try
            {
                if (this.Socket?.Connected ?? false)
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
                    return !this.Socket.Poll(1 * 1000 * 1000, SelectMode.SelectRead) ||
                           (this.Socket.Receive(new byte[1], SocketFlags.Peek) != 0);
                }
            }
            catch
            {

            }

            return false;
        }

        private bool HasEverStarted;
        protected override void InternalStart()
        {
            if (this.HasEverStarted)
            {
                throw new InvalidOperationException($"{nameof(TcpProxyClient)} cannot be started again after being stopped.");
            }

            this.HasEverStarted = true;

            this.InitDataReceived();
        }

        protected override void InternalStop()
        {
            this.TcpClient.Close();

            this.DataReceivedConnectionDisposable?.Dispose();
        }

        public static IObservable<TcpProxyClient> CreateClientConnection(IPEndPoint targetIpEndPoint)
            => CreateClientConnection(targetIpEndPoint.Address, targetIpEndPoint.Port);

        public static IObservable<TcpProxyClient> CreateClientConnection(IPAddress ipAddress, int port) =>
            Observable.Create<TcpProxyClient>(ob =>
            {
                var tcpClient = new TcpClient();
                var sub = Observable.FromAsync(() => tcpClient.ConnectAsync(ipAddress, port))
                                    .Subscribe(onNext: _ =>
                                    {
                                        var client = new TcpProxyClient(tcpClient);
                                        client.Start();
                                        ob.Respond(client);
                                    },
                                               onError: _ => ob.Respond(null));

                return sub.Dispose;
                //return () =>
                //{
                //    sub.Dispose();
                //};
            });
    }
}
