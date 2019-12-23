using System;
using System.Collections.Concurrent;
using System.Net;

namespace ReactiveNetwork.Contracts
{
    public interface IReactiveServer
    {
        ConcurrentDictionary<Guid, IReactiveClient> ConnectedClients { get; }

        IPEndPoint EndPoint { get; }

        string Name { get; }

        RunStatus Status { get; }


        IObservable<RunStatus> WhenStatusChanged();

        IObservable<IReactiveClient> WhenClientStatusChanged();

        void Start();

        void Stop();
    }
}
