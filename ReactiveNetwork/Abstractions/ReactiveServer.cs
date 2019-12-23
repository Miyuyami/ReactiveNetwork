using System;
using System.Collections.Concurrent;
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

        protected virtual bool CanOnlyStartOnce { get; } = false;
        protected bool HasEverStarted { get; set; } = false;

        public virtual ConcurrentDictionary<Guid, IReactiveClient> ConnectedClients { get; }
            = new ConcurrentDictionary<Guid, IReactiveClient>();

        protected ReactiveServer(IPAddress address, int port) : this(new IPEndPoint(address, port)) { }

        protected ReactiveServer(IPAddress address, int port, string name) : this(new IPEndPoint(address, port), name) { }

        protected ReactiveServer(IPEndPoint endPoint) : this(endPoint, String.Empty) { }

        protected ReactiveServer(IPEndPoint endPoint, string name)
        {
            this.EndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            this.Name = name;
        }

        private readonly Subject<RunStatus> StatusSubject = new Subject<RunStatus>();
        private IObservable<RunStatus> StatusChangedObservable;
        public virtual IObservable<RunStatus> WhenStatusChanged() => this.StatusChangedObservable ??=
            this.StatusSubject.StartWith(this.Status)
                              .DistinctUntilChanged()
                              .Replay(1)
                              .RefCount();

        public void Start()
        {
            if (this.CanOnlyStartOnce)
            {
                if (this.HasEverStarted)
                {
                    throw new InvalidOperationException($"{this.GetType().Name} cannot be started again after being stopped.");
                }

                this.HasEverStarted = true;
            }

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

            if (this.CanOnlyStartOnce)
            {
                this.StatusSubject.OnCompleted();
            }
        }

        protected virtual void InternalStart()
        {

        }

        protected virtual void InternalStop()
        {
            foreach (var client in this.ConnectedClients.Values)
            {
                client.Stop();
            }

            this.ConnectedClients.Clear();
        }

        public abstract IObservable<IReactiveClient> WhenClientStatusChanged();
    }
}
