using System;

namespace GenericGameServerProxy.Contracts
{
    public interface IProxyClient
    {
        ClientStatus Status { get; }

        IObservable<ClientStatus> WhenStatusChanged();

        void Start();
        void Stop();
    }
}
