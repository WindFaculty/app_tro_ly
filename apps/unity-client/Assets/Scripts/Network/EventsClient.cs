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
        private readonly SemaphoreSlim connectionGate = new(1, 1);
        private ClientWebSocket socket;
        private CancellationTokenSource lifetimeCancellation;
        private Task backgroundLoop;
        private bool disposed;
        private int reconnectAttempt;

        public EventsClient(string url)
        {
            eventsUrl = url;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            if (backgroundLoop != null)
            {
                return;
            }

            lifetimeCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await EnsureConnectedAsync(lifetimeCancellation.Token);
            backgroundLoop = RunConnectionLoopAsync(lifetimeCancellation.Token);
        }

        public bool TryDequeue(out string message) => messages.TryDequeue(out message);

        public void Dispose()
        {
            disposed = true;
            try
            {
                lifetimeCancellation?.Cancel();
            }
            catch
            {
            }

            socket?.Dispose();
            connectionGate.Dispose();
        }

        private async Task RunConnectionLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && !disposed)
            {
                try
                {
                    await ReceiveLoopAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                }

                if (disposed || cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                DisposeSocket();
                await Task.Delay(ComputeReconnectDelay(), cancellationToken);

                try
                {
                    await EnsureConnectedAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch
                {
                }
            }
        }

        private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (IsSocketConnected())
            {
                reconnectAttempt = 0;
                return;
            }

            await connectionGate.WaitAsync(cancellationToken);
            try
            {
                if (IsSocketConnected())
                {
                    reconnectAttempt = 0;
                    return;
                }

                DisposeSocket();
                socket = new ClientWebSocket();
                await socket.ConnectAsync(new Uri(eventsUrl), cancellationToken);
                reconnectAttempt = 0;
            }
            finally
            {
                connectionGate.Release();
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[4096];
            while (!cancellationToken.IsCancellationRequested && IsSocketConnected())
            {
                var builder = new StringBuilder();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        reconnectAttempt++;
                        return;
                    }

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                messages.Enqueue(builder.ToString());
            }
        }

        private bool IsSocketConnected() => socket != null && socket.State == WebSocketState.Open;

        private TimeSpan ComputeReconnectDelay()
        {
            reconnectAttempt = Math.Min(reconnectAttempt + 1, 5);
            return TimeSpan.FromSeconds(Math.Min(10, Math.Pow(2, reconnectAttempt - 1)));
        }

        private void DisposeSocket()
        {
            try
            {
                socket?.Dispose();
            }
            catch
            {
            }
            finally
            {
                socket = null;
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(EventsClient));
            }
        }
    }
}
