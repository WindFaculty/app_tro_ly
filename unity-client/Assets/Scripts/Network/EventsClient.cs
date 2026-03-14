using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LocalAssistant.Network
{
    public sealed class EventsClient : IAssistantEventsClient
    {
        private readonly string eventsUrl;
        private readonly ConcurrentQueue<string> messages = new();
        private ClientWebSocket socket;

        public EventsClient(string url)
        {
            eventsUrl = url;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(eventsUrl), cancellationToken);
            _ = ReceiveLoopAsync(cancellationToken);
        }

        public bool TryDequeue(out string message) => messages.TryDequeue(out message);

        public void Dispose()
        {
            socket?.Dispose();
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            while (!cancellationToken.IsCancellationRequested && socket != null && socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                messages.Enqueue(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
        }
    }
}
