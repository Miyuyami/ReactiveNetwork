using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using GenericGameServerProxy.Contracts;

namespace GenericGameServerProxy.Abstractions
{
    public abstract class ReactiveServer : IReactiveServer
    {
        public IPEndPoint ProxyEndPoint { get; }
        public IPEndPoint TargetEndPoint { get; }
        public string Name { get; }
        public ServerStatus Status { get; private set; }

        public abstract IReadOnlyDictionary<Guid, IReactiveClient> ConnectedClients { get; }

        public ReactiveServer(IPEndPoint proxyEndPoint, IPEndPoint targetEndPoint) : this(proxyEndPoint, targetEndPoint, String.Empty)
        {

        }

        public ReactiveServer(IPEndPoint proxyEndPoint, IPEndPoint targetEndPoint, string name)
        {
            this.ProxyEndPoint = proxyEndPoint;
            this.TargetEndPoint = targetEndPoint;
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

        public override string ToString()
        {
            return $"{this.Name} - {this.ProxyEndPoint} to {this.TargetEndPoint}";
        }

        public abstract IObservable<IReactiveClient> WhenClientStatusChanged();

        protected abstract void InternalStart();
        protected abstract void InternalStop();
    }
}
