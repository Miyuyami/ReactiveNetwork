﻿using System;

namespace ReactiveNetwork.Contracts
{
    public interface IReactiveClient
    {
        Guid Guid { get; }
        RunStatus Status { get; }


        IObservable<RunStatus> WhenStatusChanged();

        IObservable<ClientResult> WhenDataReceived();

        IObservable<ClientResult> Read();

        IObservable<ClientResult> Write(byte[] bytes);

        void WriteWithoutResponse(byte[] bytes);

        void Start();

        void Stop();
    }
}
