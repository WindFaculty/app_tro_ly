import type { AssistantStreamEvent, AssistantStreamRequest } from "@/contracts/backend";
import { getBackendBaseUrlForUi } from "@/services/backendClient";

export interface AssistantStreamConnection {
  send: (payload: AssistantStreamRequest) => void;
  close: (sendSessionStop?: boolean) => void;
  readyState: () => number;
}

interface ConnectAssistantStreamOptions {
  sessionId: string;
  onEvent: (event: AssistantStreamEvent) => void;
  onClose?: (event: CloseEvent) => void;
  onError?: (message: string) => void;
}

function buildAssistantStreamUrl(baseUrl: string): string {
  const url = new URL("/v1/assistant/stream", baseUrl);
  url.protocol = url.protocol === "https:" ? "wss:" : "ws:";
  return url.toString();
}

export async function connectAssistantStream(
  options: ConnectAssistantStreamOptions,
): Promise<AssistantStreamConnection> {
  const streamUrl = buildAssistantStreamUrl(await getBackendBaseUrlForUi());

  return new Promise<AssistantStreamConnection>((resolve, reject) => {
    const socket = new WebSocket(streamUrl);
    let opened = false;

    socket.addEventListener("open", () => {
      opened = true;
      socket.send(
        JSON.stringify({
          type: "session_start",
          session_id: options.sessionId,
          voice_mode: false,
        } satisfies AssistantStreamRequest),
      );

      resolve({
        send(payload) {
          socket.send(JSON.stringify(payload));
        },
        close(sendSessionStop = true) {
          if (sendSessionStop && socket.readyState === WebSocket.OPEN) {
            socket.send(
              JSON.stringify({
                type: "session_stop",
                session_id: options.sessionId,
                voice_mode: false,
              } satisfies AssistantStreamRequest),
            );
          }
          socket.close();
        },
        readyState() {
          return socket.readyState;
        },
      });
    });

    socket.addEventListener("message", (event) => {
      try {
        options.onEvent(JSON.parse(String(event.data)) as AssistantStreamEvent);
      } catch {
        options.onError?.("Assistant stream returned an unreadable payload.");
      }
    });

    socket.addEventListener("error", () => {
      const message = "Assistant stream could not connect to the backend.";
      if (!opened) {
        reject(new Error(message));
        return;
      }
      options.onError?.(message);
    });

    socket.addEventListener("close", (event) => {
      if (!opened && !event.wasClean) {
        reject(new Error("Assistant stream closed before the session started."));
        return;
      }
      options.onClose?.(event);
    });
  });
}
