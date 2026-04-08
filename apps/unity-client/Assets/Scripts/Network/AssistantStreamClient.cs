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
        private readonly SemaphoreSlim connectionGate = new(1, 1);
        private ClientWebSocket socket;
        private CancellationTokenSource lifetimeCancellation;
        private Task backgroundLoop;
        private bool disposed;
        private int reconnectAttempt;

        public AssistantStreamClient(string url)
        {
            streamUrl = url;
        }

        public bool IsConnected => IsSocketConnected();

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
                await socket.ConnectAsync(new Uri(streamUrl), cancellationToken);
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
                throw new ObjectDisposedException(nameof(AssistantStreamClient));
            }
        }
    }
}
