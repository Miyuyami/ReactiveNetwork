using System;
using System.Collections.Generic;
using System.Net;

namespace GenericGameServerProxy.Contracts
{
    public interface IProxyServer
    {
        IReadOnlyDictionary<Guid, IProxyClient> ConnectedClients { get; }

        IPEndPoint ProxyEndPoint { get; }

        IPEndPoint TargetEndPoint { get; }

        string Name { get; }

        ProxyServerStatus Status { get; }


        IObservable<ProxyServerStatus> WhenStatusChanged();

        IObservable<IProxyClient> WhenClientStatusChanged();

        void Start();

        void Stop();
    }
}
