using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using GenericGameServerProxy.Contracts;

namespace GenericGameServerProxy.Abstractions
{
    public abstract class ReactiveClient : IReactiveClient
    {
        public ClientStatus Status { get; private set; }

        public ReactiveClient()
        {

        }

        private Subject<ClientStatus> StatusSubject = new Subject<ClientStatus>();
        private IObservable<ClientStatus> StatusChangedObservable;
        public virtual IObservable<ClientStatus> WhenStatusChanged() => this.StatusChangedObservable = this.StatusChangedObservable ??
            this.StatusSubject
            .StartWith(this.Status)
            .DistinctUntilChanged()
            .Replay(1)
            .RefCount();

        public void Start()
        {
            if (this.Status != ClientStatus.Stopped)
            {
                return;
            }

            this.Status = ClientStatus.Starting;
            this.StatusSubject.OnNext(ClientStatus.Starting);

            this.InternalStart();

            this.Status = ClientStatus.Started;
            this.StatusSubject.OnNext(ClientStatus.Started);
        }

        public void Stop()
        {
            if (this.Status != ClientStatus.Started)
            {
                return;
            }

            this.Status = ClientStatus.Stopping;
            this.StatusSubject.OnNext(ClientStatus.Stopping);

            this.InternalStop();

            this.Status = ClientStatus.Stopped;
            this.StatusSubject.OnNext(ClientStatus.Stopped);
        }

        public abstract IObservable<ClientResult> WhenDataReceived();
        public abstract IObservable<ClientResult> Read();
        public abstract IObservable<ClientResult> Write(byte[] bytes);
        public abstract void WriteWithoutReponse(byte[] bytes);

        protected abstract void InternalStart();
        protected abstract void InternalStop();
    }
}
