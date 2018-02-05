using System;
using System.Collections.Generic;
using System.Net;

namespace ReactiveNetwork.Contracts
{
    public interface IReactiveServer
    {
        IReadOnlyDictionary<Guid, IReactiveClient> ConnectedClients { get; }

        IPEndPoint EndPoint { get; }

        string Name { get; }

        ServerStatus Status { get; }


        IObservable<ServerStatus> WhenStatusChanged();

        IObservable<IReactiveClient> WhenClientStatusChanged();

        void Start();

        void Stop();
    }
}
