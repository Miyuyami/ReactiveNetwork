using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveNetwork.Contracts;

namespace ReactiveNetwork.Abstractions
{
    public abstract class ReactiveClient : IReactiveClient
    {
        public Guid Guid { get; }
        public RunStatus Status { get; private set; }

        protected virtual bool CanOnlyStartOnce { get; } = false;
        protected bool HasEverStarted { get; set; } = false;

        protected ReactiveClient(Guid guid)
        {
            this.Guid = guid;
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

        }

        public abstract IObservable<ClientResult> WhenDataReceived();
        public abstract IObservable<ClientResult> Read();
        public abstract IObservable<ClientResult> Write(byte[] bytes);
        public abstract void WriteWithoutResponse(byte[] bytes);
    }
}
