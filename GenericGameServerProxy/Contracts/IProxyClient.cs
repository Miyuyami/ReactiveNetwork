using System;

namespace GenericGameServerProxy.Contracts
{
    public interface IProxyClient
    {
        ClientStatus Status { get; }


        IObservable<ClientStatus> WhenStatusChanged();

        IObservable<ClientResult> WhenDataReceived();

        IObservable<ClientResult> Read();

        IObservable<ClientResult> Write(byte[] bytes);

        void WriteWithoutReponse(byte[] bytes);

        void Start();

        void Stop();
    }
}
