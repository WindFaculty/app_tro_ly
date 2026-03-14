using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LocalAssistant.Network
{
    public sealed class AssistantStreamClient : IAssistantStreamClient
    {
        private readonly string streamUrl;
        private readonly ConcurrentQueue<string> messages = new();
        private ClientWebSocket socket;

        public AssistantStreamClient(string url)
        {
            streamUrl = url;
        }

        public bool IsConnected => socket != null && socket.State == WebSocketState.Open;

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(streamUrl), cancellationToken);
            _ = ReceiveLoopAsync(cancellationToken);
        }

        public async Task SendAsync(string payload, CancellationToken cancellationToken)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Assistant stream is not connected.");
            }

            var buffer = Encoding.UTF8.GetBytes(payload ?? string.Empty);
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
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
                var builder = new StringBuilder();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                messages.Enqueue(builder.ToString());
            }
        }
    }
}
