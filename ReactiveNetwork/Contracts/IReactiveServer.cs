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

        RunStatus Status { get; }


        IObservable<RunStatus> WhenStatusChanged();

        IObservable<IReactiveClient> WhenClientStatusChanged();

        void Start();

        void Stop();
    }
}
