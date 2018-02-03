namespace GenericGameServerProxy.Contracts
{
    public class ClientResult
    {
        public IReactiveClient ProxyClient { get; }
        public ClientEvent EventType { get; }
        public byte[] Data { get; }

        public ClientResult(IReactiveClient proxyClient, ClientEvent eventType, byte[] data)
        {
            this.ProxyClient = proxyClient;
            this.EventType = eventType;
            this.Data = data;
        }
    }
}
