﻿using System;
using System.Collections.Generic;
using System.Net;

namespace GenericGameServerProxy.Contracts
{
    public interface IReactiveServer
    {
        IReadOnlyDictionary<Guid, IReactiveClient> ConnectedClients { get; }

        IPEndPoint ProxyEndPoint { get; }

        IPEndPoint TargetEndPoint { get; }

        string Name { get; }

        ServerStatus Status { get; }


        IObservable<ServerStatus> WhenStatusChanged();

        IObservable<IReactiveClient> WhenClientStatusChanged();

        void Start();

        void Stop();
    }
}