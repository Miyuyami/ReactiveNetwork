using System;

namespace ReactiveNetwork.Contracts
{
    public interface IReactiveClient
    {
        RunStatus Status { get; }
        
        IObservable<RunStatus> WhenStatusChanged();

        IObservable<ClientResult> WhenDataReceived();

        IObservable<ClientResult> Read();

        IObservable<ClientResult> Write(byte[] bytes);

        void WriteWithoutReponse(byte[] bytes);

        void Start();

        void Stop();
    }
}
