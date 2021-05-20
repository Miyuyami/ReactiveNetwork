using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveNetwork.Abstractions;
using ReactiveNetwork.Contracts;

namespace ReactiveNetwork.Tcp
{
    public class TcpReactiveClient : ReactiveClient
    {
        private const int _BufferLength = 1 << 20; // 1MiB
        private readonly byte[] _Buffer = new byte[_BufferLength];

        private readonly TcpClient TcpClient;
        private readonly Socket Socket;
        private readonly NetworkStream NetworkStream;

        public virtual int RetryCount { get; set; } = 5;
        public virtual TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2d);
        public virtual TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromMinutes(1d);
        public virtual TimeSpan SendTimeout { get; set; } = TimeSpan.FromMinutes(1d);

        protected override bool CanOnlyStartOnce => true;

        public bool KeepAlive
        {
            get => Convert.ToBoolean(this.Socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive));
            set => this.Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, value);
        }

        public int KeepAliveIntervalSeconds
        {
            get => Convert.ToInt32(this.Socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval));
            set => this.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, value);
        }

        public int KeepAliveTimeSeconds
        {
            get => Convert.ToInt32(this.Socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime));
            set => this.Socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, value);
        }

        protected internal TcpReactiveClient(Guid guid, TcpClient connectedTcpClient) : base(guid)
        {
            this.TcpClient = connectedTcpClient ?? throw new ArgumentNullException(nameof(connectedTcpClient));
            this.Socket = this.TcpClient.Client;
            this.NetworkStream = this.TcpClient.GetStream();
        }

        private IObservable<int> NetworkStreamReadObservable()
        {
            int retryCount = this.RetryCount;

            return
                Observable.FromAsync(t => this.NetworkStream.ReadAsync(this._Buffer, 0, _BufferLength, t))
                          .Timeout(this.ReceiveTimeout)
                          .RetryWhen(exOb => exOb.SelectMany(ex =>
                          {
                              if (retryCount <= 0)
                              {
                                  return Observable.Throw<Unit>(ex);
                              }

                              switch (ex)
                              {
                                  case ObjectDisposedException:
                                      return Observable.Throw<Unit>(ex);
                                  case TimeoutException:
                                      return Observable.Throw<Unit>(ex);
                              }

                              retryCount--;
                              return Observable.Return(Unit.Default).Delay(this.RetryDelay);
                          }));
        }

        private IConnectableObservable<ClientResult> DataReceivedConnectableObservable;
        private IDisposable DataReceivedConnectionDisposable;
        private void InitDataReceived()
        {
            this.DataReceivedConnectableObservable =
                this.WhenStatusChanged()
                    .Where(s => s == RunStatus.Started)
                    .Take(1) // we can only start once
                    .SelectMany(Observable.While(() => this.IsConnected(), this.NetworkStreamReadObservable()))
                    .Where(i => i > 0)
                    .Select(receivedBytes =>
                    {
                        byte[] readBytes = new byte[receivedBytes];
                        Buffer.BlockCopy(this._Buffer, 0, readBytes, 0, receivedBytes);

                        return ClientResult.FromRead(this, readBytes);
                    })
                    .Finally(() => this.Stop())
                    .Publish();

            this.DataReceivedConnectionDisposable = this.DataReceivedConnectableObservable.Connect();
        }

        private IObservable<ClientResult> DataReceivedObservable;
        public override IObservable<ClientResult> WhenDataReceived() => this.DataReceivedObservable ??=
            this.DataReceivedConnectableObservable;

        public override IObservable<ClientResult> Read() =>
            this.WhenDataReceived()
                .Take(1);

        public override IObservable<ClientResult> Write(byte[] bytes) =>
            Observable.FromAsync(t => this.NetworkStream.WriteAsync(bytes, 0, bytes.Length, t))
                      .Select(_ => ClientResult.FromWrite(this, bytes, true))
                      .Timeout(this.SendTimeout)
                      .Catch<ClientResult, TimeoutException>(_ => this.FailedClientResult(bytes))
                      .Catch<ClientResult, Exception>(_ =>
                      {
                          this.StopIfNotConnected();

                          return this.FailedClientResult(bytes);
                      });

        private IObservable<ClientResult> FailedClientResult(byte[] bytes) =>
            Observable.Return(ClientResult.FromWrite(this, bytes, false));

        public override void WriteWithoutResponse(byte[] bytes) =>
            this.Write(bytes)
                .Subscribe();

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
                // ignore exceptions as we just check if we're connected
            }

            return false;
        }

        private void StopIfNotConnected()
        {
            if (!this.IsConnected())
            {
                this.Stop();
            }
        }

        protected override void InternalStart()
        {
            base.InternalStart();

            this.InitDataReceived();
        }

        protected override void InternalStop()
        {
            this.TcpClient.Close();

            this.DataReceivedConnectionDisposable?.Dispose();

            base.InternalStop();
        }

        public static IObservable<TcpReactiveClient> CreateClientConnection(IPEndPoint ipEndPoint, bool startClient = true)
            => CreateClientConnection(ipEndPoint.Address, ipEndPoint.Port, startClient);

        public static IObservable<TcpReactiveClient> CreateClientConnection(IPAddress ipAddress, int port, bool startClient = true)
            => CreateClientConnection(ipAddress, port, startClient, false, 0, 0);

        public static IObservable<TcpReactiveClient> CreateClientConnection(IPAddress ipAddress, int port, bool shouldStartClient, bool keepAlive, int keepAliveInterval, int keepAliveTime) =>
            Observable.FromAsync(async () =>
            {
                var tcpClient = new TcpClient();
                var socket = tcpClient.Client;
                SetSocketOption(socket, keepAlive, keepAliveInterval, keepAliveInterval);

                await tcpClient.ConnectAsync(ipAddress, port);
                return tcpClient;
            })
                      .Select(c =>
                      {
                          var client = new TcpReactiveClient(Guid.NewGuid(), c);
                          if (shouldStartClient)
                          {
                              client.Start();
                          }

                          return client;
                      });

        private static void SetSocketOption(Socket socket, bool keepAlive, int keepAliveInterval, int keepAliveTime)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, keepAlive);

            if (keepAliveInterval > 0)
            {
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, keepAliveInterval);
            }

            if (keepAliveTime > 0)
            {
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, keepAliveTime);
            }
        }
    }
}
