using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveNetwork.Contracts;

namespace ReactiveNetwork.Abstractions
{
    public abstract class ReactiveServer : IReactiveServer
    {
        public IPEndPoint EndPoint { get; }
        public string Name { get; }
        public ServerStatus Status { get; private set; }

        public abstract IReadOnlyDictionary<Guid, IReactiveClient> ConnectedClients { get; }

        public ReactiveServer(IPAddress address, int port) : this(new IPEndPoint(address, port))
        {

        }

        public ReactiveServer(IPAddress address, int port, string name) : this(new IPEndPoint(address, port), name)
        {

        }

        public ReactiveServer(IPEndPoint endPoint) : this(endPoint, String.Empty)
        {

        }

        public ReactiveServer(IPEndPoint endPoint, string name)
        {
            this.EndPoint = endPoint;
            this.Name = name;
        }

        private Subject<ServerStatus> StatusSubject = new Subject<ServerStatus>();
        private IObservable<ServerStatus> StatusChangedObservable;
        public virtual IObservable<ServerStatus> WhenStatusChanged() => this.StatusChangedObservable = this.StatusChangedObservable ??
            this.StatusSubject
            .StartWith(this.Status)
            .DistinctUntilChanged()
            .Replay(1)
            .RefCount();

        public void Start()
        {
            if (this.Status != ServerStatus.Stopped)
            {
                return;
            }

            this.Status = ServerStatus.Starting;
            this.StatusSubject.OnNext(ServerStatus.Starting);

            this.InternalStart();

            this.Status = ServerStatus.Started;
            this.StatusSubject.OnNext(ServerStatus.Started);
        }

        public void Stop()
        {
            if (this.Status != ServerStatus.Started)
            {
                return;
            }

            this.Status = ServerStatus.Stopping;
            this.StatusSubject.OnNext(ServerStatus.Stopping);

            this.InternalStop();

            this.Status = ServerStatus.Stopped;
            this.StatusSubject.OnNext(ServerStatus.Stopped);
        }

        public abstract IObservable<IReactiveClient> WhenClientStatusChanged();

        protected abstract void InternalStart();
        protected abstract void InternalStop();
    }
}
