namespace ReactiveNetwork.Contracts
{
    public class ClientResult
    {
        public IReactiveClient Client { get; }
        public ClientEvent EventType { get; }
        public byte[] Data { get; }
        public bool Success { get; }

        private ClientResult(IReactiveClient client, ClientEvent eventType, byte[] data, bool success)
        {
            this.Client = client;
            this.EventType = eventType;
            this.Data = data;
            this.Success = success;
        }

        public static ClientResult FromRead(IReactiveClient client, byte[] data, bool success = true)
            => new ClientResult(client, ClientEvent.Read, data, success);

        public static ClientResult FromWrite(IReactiveClient client, byte[] data, bool success)
            => new ClientResult(client, ClientEvent.Write, data, success);
    }
}
