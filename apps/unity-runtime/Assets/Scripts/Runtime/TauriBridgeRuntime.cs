using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LocalAssistant.Runtime
{
    public sealed class TauriBridgeRuntime : MonoBehaviour
    {
        [SerializeField] private string bridgeUrl = "ws://127.0.0.1:7857/unity-bridge";

        private readonly ConcurrentQueue<UnityBridgeCommandEnvelope> inboundCommands = new ConcurrentQueue<UnityBridgeCommandEnvelope>();
        private readonly ConcurrentQueue<string> outboundMessages = new ConcurrentQueue<string>();

        private CancellationTokenSource cancellation;
        private ClientWebSocket socket;
        private UnityBridgeClient unityBridgeClient;
        private AvatarRuntime avatarRuntime;
        private InteractionRuntime interactionRuntime;

        public void Bind(
            UnityBridgeClient bridgeClient,
            AvatarRuntime avatar,
            InteractionRuntime interaction)
        {
            unityBridgeClient = bridgeClient;
            avatarRuntime = avatar;
            interactionRuntime = interaction;

            if (unityBridgeClient != null)
            {
                unityBridgeClient.CommandRejected += HandleCommandRejected;
            }

            if (avatarRuntime != null)
            {
                avatarRuntime.StateChanged += HandleAvatarStateChanged;
            }

            if (interactionRuntime != null)
            {
                interactionRuntime.ObjectFocused += HandleObjectFocused;
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            cancellation = new CancellationTokenSource();
            _ = RunBridgeLoopAsync(cancellation.Token);
        }

        private void OnDisable()
        {
            if (unityBridgeClient != null)
            {
                unityBridgeClient.CommandRejected -= HandleCommandRejected;
            }

            if (avatarRuntime != null)
            {
                avatarRuntime.StateChanged -= HandleAvatarStateChanged;
            }

            if (interactionRuntime != null)
            {
                interactionRuntime.ObjectFocused -= HandleObjectFocused;
            }

            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
            _ = CloseSocketAsync();
        }

        private void Update()
        {
            if (unityBridgeClient == null)
            {
                return;
            }

            while (inboundCommands.TryDequeue(out var command))
            {
                if (!unityBridgeClient.ApplyEnvelope(command))
                {
                    QueueEvent("bridge.error", new UnityBridgeEventPayload
                    {
                        message = "Unity runtime tu choi command typed.",
                        detail = command.type,
                    });
                }
            }
        }

        private async Task RunBridgeLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                socket = new ClientWebSocket();

                try
                {
                    await socket.ConnectAsync(new Uri(bridgeUrl), token);
                    QueueEvent("bridge.ready", new UnityBridgeEventPayload
                    {
                        transport = "local_websocket",
                        url = bridgeUrl,
                    });

                    var receiveTask = ReceiveLoopAsync(socket, token);
                    var sendTask = SendLoopAsync(socket, token);
                    await Task.WhenAny(receiveTask, sendTask);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[TauriBridgeRuntime] Bridge connect failed: {exception.Message}");
                }
                finally
                {
                    await CloseSocketAsync();
                }

                try
                {
                    await Task.Delay(1500, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task ReceiveLoopAsync(ClientWebSocket activeSocket, CancellationToken token)
        {
            var buffer = new byte[16 * 1024];

            while (!token.IsCancellationRequested && activeSocket.State == WebSocketState.Open)
            {
                var builder = new StringBuilder();
                WebSocketReceiveResult result;

                do
                {
                    result = await activeSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    builder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                var json = builder.ToString();
                try
                {
                    inboundCommands.Enqueue(LocalAssistant.Core.UnityJson.Deserialize<UnityBridgeCommandEnvelope>(json));
                }
                catch (Exception exception)
                {
                    QueueEvent("bridge.error", new UnityBridgeEventPayload
                    {
                        message = "Unity parse command failed.",
                        detail = exception.Message,
                    });
                }
            }
        }

        private async Task SendLoopAsync(ClientWebSocket activeSocket, CancellationToken token)
        {
            while (!token.IsCancellationRequested && activeSocket.State == WebSocketState.Open)
            {
                while (outboundMessages.TryDequeue(out var message))
                {
                    var buffer = Encoding.UTF8.GetBytes(message);
                    await activeSocket.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        token);
                }

                await Task.Delay(50, token);
            }
        }

        private void HandleCommandRejected(string reason)
        {
            QueueEvent("bridge.error", new UnityBridgeEventPayload
            {
                message = reason,
                detail = "UnityBridgeClient rejected a typed command.",
            });
        }

        private void HandleAvatarStateChanged(string state)
        {
            QueueEvent("avatar.stateChanged", new UnityBridgeEventPayload
            {
                state = state,
            });
        }

        private void HandleObjectFocused(string objectName)
        {
            QueueEvent("room.interactionTriggered", new UnityBridgeEventPayload
            {
                interaction = "focus_object",
                object_name = objectName,
            });
        }

        private void QueueEvent(string eventType, UnityBridgeEventPayload payload)
        {
            outboundMessages.Enqueue(LocalAssistant.Core.UnityJson.Serialize(new UnityBridgeEventEnvelope
            {
                id = Guid.NewGuid().ToString("N"),
                type = eventType,
                source = "unity",
                timestamp = DateTime.UtcNow.ToString("O"),
                payload = payload ?? new UnityBridgeEventPayload(),
            }));
        }

        private async Task CloseSocketAsync()
        {
            if (socket == null)
            {
                return;
            }

            try
            {
                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
                }
            }
            catch
            {
            }
            finally
            {
                socket.Dispose();
                socket = null;
            }
        }
    }
}
