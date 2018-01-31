namespace GenericGameServerProxy.Contracts
{
    public class ClientResult
    {
        public IProxyClient ProxyClient { get; }
        public ClientEvent EventType { get; }
        public byte[] Data { get; }

        public ClientResult(IProxyClient proxyClient, ClientEvent eventType, byte[] data)
        {
            this.ProxyClient = proxyClient;
            this.EventType = eventType;
            this.Data = data;
        }
    }
}
