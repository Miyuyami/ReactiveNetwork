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
        public RunStatus Status { get; private set; }

        public abstract IReadOnlyDictionary<Guid, IReactiveClient> ConnectedClients { get; }

        public ReactiveServer(IPAddress address, int port) : this(new IPEndPoint(address, port)) { }

        public ReactiveServer(IPAddress address, int port, string name) : this(new IPEndPoint(address, port), name) { }

        public ReactiveServer(IPEndPoint endPoint) : this(endPoint, String.Empty) { }

        public ReactiveServer(IPEndPoint endPoint, string name)
        {
            this.EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            this.Name = name;
        }

        private Subject<RunStatus> StatusSubject = new Subject<RunStatus>();
        private IObservable<RunStatus> StatusChangedObservable;
        public virtual IObservable<RunStatus> WhenStatusChanged() => this.StatusChangedObservable = this.StatusChangedObservable ??
            this.StatusSubject
            .StartWith(this.Status)
            .DistinctUntilChanged()
            .Replay(1)
            .RefCount();

        public void Start()
        {
            if (this.Status != RunStatus.Stopped)
            {
                return;
            }

            this.Status = RunStatus.Starting;
            this.StatusSubject.OnNext(RunStatus.Starting);

            this.InternalStart();

            this.Status = RunStatus.Started;
            this.StatusSubject.OnNext(RunStatus.Started);
        }

        public void Stop()
        {
            if (this.Status != RunStatus.Started)
            {
                return;
            }

            this.Status = RunStatus.Stopping;
            this.StatusSubject.OnNext(RunStatus.Stopping);

            this.InternalStop();

            this.Status = RunStatus.Stopped;
            this.StatusSubject.OnNext(RunStatus.Stopped);
        }

        public abstract IObservable<IReactiveClient> WhenClientStatusChanged();

        protected abstract void InternalStart();
        protected abstract void InternalStop();
    }
}
