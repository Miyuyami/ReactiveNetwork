using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using GenericGameServerProxy.Contracts;

namespace GenericGameServerProxy.Abstractions
{
    public abstract class ProxyServer : IProxyServer
    {
        public IPEndPoint ProxyEndPoint { get; }
        public IPEndPoint TargetEndPoint { get; }
        public string Name { get; }
        public ProxyServerStatus Status { get; private set; }

        public abstract IReadOnlyDictionary<Guid, IProxyClient> ConnectedClients { get; }

        public ProxyServer(IPEndPoint proxyEndPoint, IPEndPoint targetEndPoint) : this(proxyEndPoint, targetEndPoint, String.Empty)
        {

        }

        public ProxyServer(IPEndPoint proxyEndPoint, IPEndPoint targetEndPoint, string name)
        {
            this.ProxyEndPoint = proxyEndPoint;
            this.TargetEndPoint = targetEndPoint;
            this.Name = name;
        }

        private Subject<ProxyServerStatus> StatusSubject = new Subject<ProxyServerStatus>();
        private IObservable<ProxyServerStatus> StatusChangedObservable;
        public virtual IObservable<ProxyServerStatus> WhenStatusChanged() => this.StatusChangedObservable = this.StatusChangedObservable ??
            this.StatusSubject
            .StartWith(this.Status)
            .DistinctUntilChanged()
            .Replay(1)
            .RefCount();

        public void Start()
        {
            if (this.Status != ProxyServerStatus.Stopped)
            {
                return;
            }

            this.Status = ProxyServerStatus.Starting;
            this.StatusSubject.OnNext(ProxyServerStatus.Starting);

            this.InternalStart();

            this.Status = ProxyServerStatus.Started;
            this.StatusSubject.OnNext(ProxyServerStatus.Started);
        }

        public void Stop()
        {
            if (this.Status != ProxyServerStatus.Started)
            {
                return;
            }

            this.Status = ProxyServerStatus.Stopping;
            this.StatusSubject.OnNext(ProxyServerStatus.Stopping);

            this.InternalStop();

            this.Status = ProxyServerStatus.Stopped;
            this.StatusSubject.OnNext(ProxyServerStatus.Stopped);
        }

        public override string ToString()
        {
            return $"{this.Name} - {this.ProxyEndPoint} to {this.TargetEndPoint}";
        }

        public abstract IObservable<IProxyClient> WhenClientStatusChanged();

        protected abstract void InternalStart();
        protected abstract void InternalStop();
    }
}
