import { startTransition, useEffect, useRef, useState } from "react";
import type {
  AssistantFinalEvent,
  AssistantStreamEvent,
  ChatActionReport,
  ChatConversationSummary,
  ChatMessageRecord,
  ChatResponse,
  VoiceSettings,
} from "@/contracts/backend";
import { createPushToTalkRecorder, type PushToTalkRecorder } from "@/features/chat/pushToTalkRecorder";
import {
  getChatConversation,
  getSettings,
  listChatConversations,
  resolveBackendAssetUrl,
  sendChatMessage,
  transcribeSpeechAudio,
} from "@/services/backendClient";
import {
  connectAssistantStream,
  type AssistantStreamConnection,
} from "@/services/assistantStream";

const THREAD_LIMIT = 24;

type StreamMode = "idle" | "stream" | "fallback";
type MicrophoneState =
  | "unsupported"
  | "idle"
  | "requesting_permission"
  | "listening"
  | "processing";
type SpeechPlaybackState = "idle" | "playing";

interface ConversationSignals {
  route: string;
  provider: string;
  latencyMs: number | null;
  fallbackCount: number;
  latestCards: Array<Record<string, unknown>>;
  latestTaskActions: ChatActionReport[];
}

interface SpeechQueueItem {
  audioUrl: string;
  text: string;
  durationMs: number;
}

const DEFAULT_VOICE_SETTINGS: VoiceSettings = {
  input_mode: "push_to_talk",
  tts_voice: "vi-VN-default",
  speak_replies: true,
  show_transcript_preview: true,
};

export interface AssistantWorkspaceState {
  threads: ChatConversationSummary[];
  activeConversationId: string | null;
  messages: ChatMessageRecord[];
  assistantDraft: string;
  assistantState: string;
  route: string;
  provider: string;
  latencyMs: number | null;
  fallbackCount: number;
  latestCards: Array<Record<string, unknown>>;
  latestTaskActions: ChatActionReport[];
  lastError: string;
  lastSubmittedMessage: string;
  streamMode: StreamMode;
  loadingThreads: boolean;
  loadingConversation: boolean;
  submitting: boolean;
  voiceInputMode: string;
  showTranscriptPreview: boolean;
  voiceSupported: boolean;
  microphoneState: MicrophoneState;
  speechPlaybackState: SpeechPlaybackState;
  transcriptPreview: string;
  voiceDurationMs: number;
  audioQueueDepth: number;
}

export interface AssistantWorkspaceApi extends AssistantWorkspaceState {
  selectConversation: (conversationId: string) => Promise<void>;
  startNewConversation: () => void;
  submitMessage: (message: string) => Promise<void>;
  retryLastTurn: () => Promise<void>;
  refreshThreads: () => Promise<void>;
  beginVoiceCapture: () => Promise<void>;
  endVoiceCapture: () => Promise<void>;
  stopSpeechPlayback: () => void;
}

function createUiId(prefix: string): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return `${prefix}_${crypto.randomUUID()}`;
  }

  return `${prefix}_${Date.now()}_${Math.round(Math.random() * 1000)}`;
}

function nowIso(): string {
  return new Date().toISOString();
}

function supportsVoiceCapture(): boolean {
  if (typeof window === "undefined" || typeof navigator === "undefined") {
    return false;
  }

  const browserWindow = window as Window & {
    webkitAudioContext?: typeof AudioContext;
  };

  return Boolean(
    typeof navigator.mediaDevices?.getUserMedia === "function" &&
      (typeof AudioContext !== "undefined" || browserWindow.webkitAudioContext),
  );
}

function buildOptimisticUserMessage(
  message: string,
  conversationId: string | null,
): ChatMessageRecord {
  return {
    id: createUiId("msg"),
    conversation_id: conversationId ?? "pending",
    role: "user",
    content: message,
    emotion: null,
    animation_hint: null,
    metadata: { source: "web_ui_optimistic" },
    created_at: nowIso(),
  };
}

function buildAssistantMessageFromFinal(event: AssistantFinalEvent): ChatMessageRecord {
  return {
    id: createUiId("msg"),
    conversation_id: event.conversation_id,
    role: "assistant",
    content: event.reply_text,
    emotion: null,
    animation_hint: null,
    metadata: {
      route: event.route ?? "",
      provider: event.provider ?? "",
      latency_ms: event.latency_ms ?? null,
      token_usage: event.token_usage,
      fallback_used: event.fallback_used,
      plan_id: event.plan_id ?? null,
      cards: event.cards,
      task_actions: event.task_actions,
      source: "assistant_stream",
    },
    created_at: nowIso(),
  };
}

function buildAssistantMessageFromResponse(response: ChatResponse): ChatMessageRecord {
  return {
    id: createUiId("msg"),
    conversation_id: response.conversation_id,
    role: "assistant",
    content: response.reply_text,
    emotion: response.emotion,
    animation_hint: response.animation_hint,
    metadata: {
      route: response.route ?? "",
      provider: response.provider ?? "",
      latency_ms: response.latency_ms ?? null,
      token_usage: response.token_usage,
      fallback_used: response.fallback_used,
      plan_id: response.plan_id ?? null,
      cards: response.cards,
      task_actions: response.task_actions,
      source: "chat_rest_fallback",
    },
    created_at: nowIso(),
  };
}

function readString(value: unknown): string {
  return typeof value === "string" ? value : "";
}

function readNumber(value: unknown): number | null {
  return typeof value === "number" ? value : null;
}

function readBoolean(value: unknown): boolean {
  return value === true;
}

function readCards(metadata: Record<string, unknown>): Array<Record<string, unknown>> {
  return Array.isArray(metadata.cards)
    ? metadata.cards.filter((item): item is Record<string, unknown> => Boolean(item) && typeof item === "object")
    : [];
}

function readTaskActions(metadata: Record<string, unknown>): ChatActionReport[] {
  return Array.isArray(metadata.task_actions)
    ? metadata.task_actions.filter(
        (item): item is ChatActionReport =>
          Boolean(item) &&
          typeof item === "object" &&
          "type" in item &&
          "status" in item,
      )
    : [];
}

function extractConversationSignals(messages: ChatMessageRecord[]): ConversationSignals {
  let route = "";
  let provider = "";
  let latencyMs: number | null = null;
  let fallbackCount = 0;
  let latestCards: Array<Record<string, unknown>> = [];
  let latestTaskActions: ChatActionReport[] = [];

  for (const message of messages) {
    if (message.role !== "assistant") {
      continue;
    }

    if (readBoolean(message.metadata.fallback_used)) {
      fallbackCount += 1;
    }
  }

  for (let index = messages.length - 1; index >= 0; index -= 1) {
    const message = messages[index];
    if (message.role !== "assistant") {
      continue;
    }

    route = readString(message.metadata.route);
    provider = readString(message.metadata.provider);
    latencyMs = readNumber(message.metadata.latency_ms);
    latestCards = readCards(message.metadata);
    latestTaskActions = readTaskActions(message.metadata);
    break;
  }

  return {
    route,
    provider,
    latencyMs,
    fallbackCount,
    latestCards,
    latestTaskActions,
  };
}

function replacePendingConversationId(
  messages: ChatMessageRecord[],
  conversationId: string,
): ChatMessageRecord[] {
  return messages.map((message) =>
    message.conversation_id === "pending"
      ? { ...message, conversation_id: conversationId }
      : message,
  );
}

function blobToBase64(blob: Blob): Promise<string> {
  return new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      if (typeof reader.result !== "string") {
        reject(new Error("Voice capture could not be encoded for streaming."));
        return;
      }

      const [, base64 = ""] = reader.result.split(",", 2);
      resolve(base64);
    };
    reader.onerror = () => {
      reject(new Error("Voice capture could not be encoded for streaming."));
    };
    reader.readAsDataURL(blob);
  });
}

export function useAssistantWorkspace(): AssistantWorkspaceApi {
  const voiceSupported = supportsVoiceCapture();
  const [threads, setThreads] = useState<ChatConversationSummary[]>([]);
  const [activeConversationId, setActiveConversationId] = useState<string | null>(null);
  const [messages, setMessages] = useState<ChatMessageRecord[]>([]);
  const [assistantDraft, setAssistantDraft] = useState("");
  const [assistantState, setAssistantState] = useState("idle");
  const [route, setRoute] = useState("");
  const [provider, setProvider] = useState("");
  const [latencyMs, setLatencyMs] = useState<number | null>(null);
  const [fallbackCount, setFallbackCount] = useState(0);
  const [latestCards, setLatestCards] = useState<Array<Record<string, unknown>>>([]);
  const [latestTaskActions, setLatestTaskActions] = useState<ChatActionReport[]>([]);
  const [lastError, setLastError] = useState("");
  const [lastSubmittedMessage, setLastSubmittedMessage] = useState("");
  const [streamMode, setStreamMode] = useState<StreamMode>("idle");
  const [loadingThreads, setLoadingThreads] = useState(true);
  const [loadingConversation, setLoadingConversation] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [voiceSettings, setVoiceSettings] = useState<VoiceSettings>(DEFAULT_VOICE_SETTINGS);
  const [microphoneState, setMicrophoneState] = useState<MicrophoneState>(
    voiceSupported ? "idle" : "unsupported",
  );
  const [speechPlaybackState, setSpeechPlaybackState] =
    useState<SpeechPlaybackState>("idle");
  const [transcriptPreview, setTranscriptPreview] = useState("");
  const [voiceDurationMs, setVoiceDurationMs] = useState(0);
  const [audioQueueDepth, setAudioQueueDepth] = useState(0);

  const mountedRef = useRef(true);
  const connectionRef = useRef<AssistantStreamConnection | null>(null);
  const connectionPromiseRef = useRef<Promise<AssistantStreamConnection> | null>(null);
  const recorderRef = useRef<PushToTalkRecorder | null>(null);
  const audioQueueRef = useRef<SpeechQueueItem[]>([]);
  const audioPlayerRef = useRef<HTMLAudioElement | null>(null);
  const audioDrainPromiseRef = useRef<Promise<void> | null>(null);
  const audioSessionRef = useRef(0);
  const sessionIdRef = useRef(createUiId("sess"));
  const activeConversationIdRef = useRef<string | null>(null);
  const lastSubmittedMessageRef = useRef("");
  const submittingRef = useRef(false);
  const microphoneStateRef = useRef<MicrophoneState>(voiceSupported ? "idle" : "unsupported");
  const voiceTransportRef = useRef<StreamMode>("idle");

  useEffect(() => {
    activeConversationIdRef.current = activeConversationId;
  }, [activeConversationId]);

  useEffect(() => {
    lastSubmittedMessageRef.current = lastSubmittedMessage;
  }, [lastSubmittedMessage]);

  useEffect(() => {
    submittingRef.current = submitting;
  }, [submitting]);

  useEffect(() => {
    microphoneStateRef.current = microphoneState;
  }, [microphoneState]);

  const stopSpeechPlayback = () => {
    audioSessionRef.current += 1;
    audioQueueRef.current = [];
    setAudioQueueDepth(0);

    const audioPlayer = audioPlayerRef.current;
    if (audioPlayer) {
      audioPlayer.pause();
      audioPlayer.removeAttribute("src");
      audioPlayer.load();
      audioPlayerRef.current = null;
    }

    if (mountedRef.current) {
      setSpeechPlaybackState("idle");
    }
  };

  const playSpeechQueue = () => {
    if (audioDrainPromiseRef.current) {
      return;
    }

    const session = audioSessionRef.current;
    audioDrainPromiseRef.current = (async () => {
      while (
        mountedRef.current &&
        session === audioSessionRef.current &&
        audioQueueRef.current.length > 0
      ) {
        const nextClip = audioQueueRef.current.shift();
        if (!nextClip) {
          continue;
        }

        setAudioQueueDepth(audioQueueRef.current.length);
        setSpeechPlaybackState("playing");

        try {
          const resolvedUrl = await resolveBackendAssetUrl(nextClip.audioUrl);
          if (!mountedRef.current || session !== audioSessionRef.current) {
            return;
          }

          const audio = new Audio(resolvedUrl);
          audio.preload = "auto";
          audioPlayerRef.current = audio;

          await new Promise<void>((resolve, reject) => {
            const cleanup = () => {
              audio.removeEventListener("ended", handleEnded);
              audio.removeEventListener("error", handleError);
            };

            const handleEnded = () => {
              cleanup();
              resolve();
            };

            const handleError = () => {
              cleanup();
              reject(new Error(`Audio playback failed for "${nextClip.text}".`));
            };

            audio.addEventListener("ended", handleEnded);
            audio.addEventListener("error", handleError);
            const playPromise = audio.play();
            if (playPromise) {
              playPromise.catch((error: unknown) => {
                cleanup();
                reject(
                  error instanceof Error
                    ? error
                    : new Error(`Audio playback failed for "${nextClip.text}".`),
                );
              });
            }
          });
        } catch (error) {
          if (mountedRef.current && session === audioSessionRef.current) {
            setLastError(error instanceof Error ? error.message : String(error));
          }
          break;
        } finally {
          if (audioPlayerRef.current) {
            audioPlayerRef.current.pause();
            audioPlayerRef.current.removeAttribute("src");
            audioPlayerRef.current.load();
            audioPlayerRef.current = null;
          }
        }
      }
    })()
      .finally(() => {
        audioDrainPromiseRef.current = null;
        if (mountedRef.current && session === audioSessionRef.current) {
          setAudioQueueDepth(audioQueueRef.current.length);
          setSpeechPlaybackState("idle");
        }
      });
  };

  const enqueueSpeechClip = (clip: SpeechQueueItem) => {
    audioQueueRef.current.push(clip);
    setAudioQueueDepth(audioQueueRef.current.length);
    playSpeechQueue();
  };

  const refreshVoiceSettings = async () => {
    const settings = await getSettings();
    if (!mountedRef.current) {
      return;
    }

    setVoiceSettings({
      ...DEFAULT_VOICE_SETTINGS,
      ...settings.voice,
      input_mode: "push_to_talk",
    });
  };

  const syncSignalsFromMessages = (nextMessages: ChatMessageRecord[]) => {
    const signals = extractConversationSignals(nextMessages);
    setRoute(signals.route);
    setProvider(signals.provider);
    setLatencyMs(signals.latencyMs);
    setFallbackCount(signals.fallbackCount);
    setLatestCards(signals.latestCards);
    setLatestTaskActions(signals.latestTaskActions);
  };

  const clearVoiceFeedback = () => {
    setTranscriptPreview("");
    setVoiceDurationMs(0);
  };

  const applyConversation = (conversationId: string | null, nextMessages: ChatMessageRecord[]) => {
    startTransition(() => {
      setActiveConversationId(conversationId);
      setMessages(nextMessages);
      setAssistantDraft("");
      setAssistantState("idle");
      setLastError("");
      clearVoiceFeedback();
      syncSignalsFromMessages(nextMessages);
    });
  };

  const loadConversation = async (conversationId: string) => {
    setLoadingConversation(true);

    try {
      const detail = await getChatConversation(conversationId);
      if (!mountedRef.current) {
        return;
      }

      applyConversation(detail.conversation_id, detail.messages);
    } catch (error) {
      if (mountedRef.current) {
        setLastError(error instanceof Error ? error.message : String(error));
      }
    } finally {
      if (mountedRef.current) {
        setLoadingConversation(false);
      }
    }
  };

  const refreshThreadList = async (preferredConversationId?: string | null) => {
    const list = await listChatConversations(THREAD_LIMIT);
    if (!mountedRef.current) {
      return list.items;
    }

    startTransition(() => {
      setThreads(list.items);
    });

    const nextConversationId =
      preferredConversationId === undefined
        ? activeConversationIdRef.current ?? list.items[0]?.conversation_id ?? null
        : preferredConversationId;

    if (nextConversationId) {
      await loadConversation(nextConversationId);
    } else if (preferredConversationId === null) {
      applyConversation(null, []);
    }

    return list.items;
  };

  const eventHandlerRef = useRef<(event: AssistantStreamEvent) => void>(() => undefined);

  eventHandlerRef.current = (event: AssistantStreamEvent) => {
    switch (event.type) {
      case "assistant_state_changed":
        setAssistantState(event.state);
        break;
      case "route_selected":
        setRoute(event.route);
        setProvider(event.provider ?? "");
        break;
      case "transcript_partial":
        setTranscriptPreview(event.text);
        break;
      case "transcript_final": {
        const transcript = event.text.trim();
        setTranscriptPreview(transcript);
        setVoiceDurationMs(0);

        if (!transcript) {
          setSubmitting(false);
          setAssistantState("idle");
          setMicrophoneState(voiceSupported ? "idle" : "unsupported");
          setLastError("No speech was detected. Hold the button and try again.");
          break;
        }

        setLastSubmittedMessage(transcript);
        setMessages((current) => [
          ...current,
          buildOptimisticUserMessage(transcript, activeConversationIdRef.current),
        ]);
        break;
      }
      case "assistant_chunk":
        setAssistantDraft((current) =>
          current ? `${current} ${event.text.trim()}`.trim() : event.text.trim(),
        );
        break;
      case "task_action_applied":
        setLatestTaskActions((current) => [...current, event.action].slice(-6));
        break;
      case "speech_started":
        break;
      case "tts_sentence_ready":
        enqueueSpeechClip({
          audioUrl: event.audio_url,
          text: event.text,
          durationMs: event.duration_ms,
        });
        break;
      case "speech_finished":
        if (audioQueueRef.current.length === 0 && !audioPlayerRef.current) {
          setSpeechPlaybackState("idle");
        }
        break;
      case "assistant_final": {
        const assistantMessage = buildAssistantMessageFromFinal(event);
        startTransition(() => {
          setStreamMode("stream");
          setSubmitting(false);
          setLastError("");
          setActiveConversationId(event.conversation_id);
          setMessages((current) => [
            ...replacePendingConversationId(current, event.conversation_id),
            assistantMessage,
          ]);
          setAssistantDraft("");
          setRoute(event.route ?? "");
          setProvider(event.provider ?? "");
          setLatencyMs(event.latency_ms ?? null);
          setFallbackCount((current) => current + (event.fallback_used ? 1 : 0));
          setLatestCards(event.cards);
          setLatestTaskActions(event.task_actions);
          setMicrophoneState(voiceSupported ? "idle" : "unsupported");
        });
        void refreshThreadList(event.conversation_id);
        break;
      }
      case "error":
        setSubmitting(false);
        setAssistantState("idle");
        setMicrophoneState(voiceSupported ? "idle" : "unsupported");
        setLastError(event.detail);
        break;
      default:
        break;
    }
  };

  const ensureConnection = async (): Promise<AssistantStreamConnection> => {
    if (connectionRef.current && connectionRef.current.readyState() === WebSocket.OPEN) {
      return connectionRef.current;
    }

    if (connectionPromiseRef.current) {
      return connectionPromiseRef.current;
    }

    const promise = connectAssistantStream({
      sessionId: sessionIdRef.current,
      onEvent(event) {
        if (mountedRef.current) {
          eventHandlerRef.current(event);
        }
      },
      onClose() {
        connectionRef.current = null;
        connectionPromiseRef.current = null;

        if (mountedRef.current && submittingRef.current) {
          setSubmitting(false);
          setAssistantState("idle");
          setMicrophoneState(voiceSupported ? "idle" : "unsupported");
          setLastError("Assistant stream disconnected before the reply finished. Retry the last turn.");
        }
      },
      onError(message) {
        if (mountedRef.current) {
          setLastError(message);
        }
      },
    })
      .then((connection) => {
        connectionRef.current = connection;
        return connection;
      })
      .finally(() => {
        connectionPromiseRef.current = null;
      });

    connectionPromiseRef.current = promise;
    return promise;
  };

  const submitViaRest = async (
    message: string,
    options: { includeVoice: boolean; voiceMode: boolean },
  ) => {
    const response = await sendChatMessage({
      message,
      conversation_id: activeConversationIdRef.current,
      session_id: sessionIdRef.current,
      mode: options.voiceMode ? "voice" : "text",
      include_voice: options.includeVoice,
      voice_mode: options.voiceMode,
    });

    if (!mountedRef.current) {
      return;
    }

    const assistantMessage = buildAssistantMessageFromResponse(response);

    startTransition(() => {
      setStreamMode("fallback");
      setSubmitting(false);
      setLastError("");
      setAssistantState("idle");
      setActiveConversationId(response.conversation_id);
      setMessages((current) => [
        ...replacePendingConversationId(current, response.conversation_id),
        assistantMessage,
      ]);
      setAssistantDraft("");
      setRoute(response.route ?? "");
      setProvider(response.provider ?? "");
      setLatencyMs(response.latency_ms ?? null);
      setFallbackCount((current) => current + (response.fallback_used ? 1 : 0));
      setLatestCards(response.cards);
      setLatestTaskActions(response.task_actions);
      setMicrophoneState(voiceSupported ? "idle" : "unsupported");
    });

    if (options.includeVoice && response.speak && response.audio_url) {
      enqueueSpeechClip({
        audioUrl: response.audio_url,
        text: response.reply_text,
        durationMs: 0,
      });
    }

    await refreshThreadList(response.conversation_id);
  };

  const submitVoiceFallback = async (audioBlob: Blob) => {
    const transcript = await transcribeSpeechAudio(audioBlob);
    const transcriptText = transcript.text.trim();

    if (!transcriptText) {
      setSubmitting(false);
      setAssistantState("idle");
      setMicrophoneState(voiceSupported ? "idle" : "unsupported");
      setLastError("No speech was detected. Hold the button and try again.");
      return;
    }

    setTranscriptPreview(transcriptText);
    setLastSubmittedMessage(transcriptText);
    setMessages((current) => [
      ...current,
      buildOptimisticUserMessage(transcriptText, activeConversationIdRef.current),
    ]);
    await submitViaRest(transcriptText, { includeVoice: true, voiceMode: true });
  };

  const submitMessage = async (message: string) => {
    const normalizedMessage = message.trim();
    if (!normalizedMessage || submittingRef.current) {
      return;
    }

    stopSpeechPlayback();
    clearVoiceFeedback();
    setLastSubmittedMessage(normalizedMessage);
    setLastError("");
    setSubmitting(true);
    setAssistantState("thinking");
    setAssistantDraft("");
    setLatestCards([]);
    setLatestTaskActions([]);
    setMessages((current) => [
      ...current,
      buildOptimisticUserMessage(normalizedMessage, activeConversationIdRef.current),
    ]);

    try {
      const connection = await ensureConnection();
      connection.send({
        type: "text_turn",
        session_id: sessionIdRef.current,
        conversation_id: activeConversationIdRef.current,
        message: normalizedMessage,
        voice_mode: false,
      });
    } catch {
      await submitViaRest(normalizedMessage, { includeVoice: false, voiceMode: false });
    }
  };

  const retryLastTurn = async () => {
    if (!lastSubmittedMessageRef.current || submittingRef.current) {
      return;
    }

    await submitMessage(lastSubmittedMessageRef.current);
  };

  const beginVoiceCapture = async () => {
    if (submittingRef.current || microphoneStateRef.current === "listening") {
      return;
    }

    if (!voiceSupported) {
      setMicrophoneState("unsupported");
      setLastError("Microphone capture is not available in this runtime.");
      return;
    }

    stopSpeechPlayback();
    clearVoiceFeedback();
    setLastError("");
    setAssistantDraft("");
    setLatestCards([]);
    setLatestTaskActions([]);
    setAssistantState("listening");
    setMicrophoneState("requesting_permission");

    let connection: AssistantStreamConnection | null = null;
    try {
      connection = await ensureConnection();
      voiceTransportRef.current = "stream";
      connection.send({
        type: "context_update",
        session_id: sessionIdRef.current,
        conversation_id: activeConversationIdRef.current,
        voice_mode: true,
      });
    } catch {
      voiceTransportRef.current = "fallback";
    }

    if (!recorderRef.current) {
      recorderRef.current = createPushToTalkRecorder({
        onChunk: async (chunk) => {
          if (
            voiceTransportRef.current !== "stream" ||
            !connectionRef.current ||
            connectionRef.current.readyState() !== WebSocket.OPEN
          ) {
            return;
          }

          const audioBase64 = await blobToBase64(chunk);
          connectionRef.current.send({
            type: "voice_chunk",
            session_id: sessionIdRef.current,
            conversation_id: activeConversationIdRef.current,
            voice_mode: true,
            audio_base64: audioBase64,
          });
        },
        onDuration(durationMs) {
          if (mountedRef.current) {
            setVoiceDurationMs(durationMs);
          }
        },
      });
    }

    try {
      await recorderRef.current.start();
      if (!mountedRef.current) {
        return;
      }

      setMicrophoneState("listening");
      setStreamMode(voiceTransportRef.current);
    } catch (error) {
      await recorderRef.current.cancel();
      if (connection) {
        connection.send({
          type: "cancel_response",
          session_id: sessionIdRef.current,
          conversation_id: activeConversationIdRef.current,
          voice_mode: true,
        });
      }

      if (mountedRef.current) {
        setMicrophoneState("idle");
        setAssistantState("idle");
        setLastError(error instanceof Error ? error.message : String(error));
      }
    }
  };

  const endVoiceCapture = async () => {
    if (!recorderRef.current || !recorderRef.current.isRecording()) {
      return;
    }

    setSubmitting(true);
    setMicrophoneState("processing");

    try {
      const capture = await recorderRef.current.stop();
      if (!capture.fullBlob) {
        setSubmitting(false);
        setAssistantState("idle");
        setMicrophoneState(voiceSupported ? "idle" : "unsupported");
        setLastError("No speech was captured. Hold the button and try again.");
        return;
      }

      if (
        voiceTransportRef.current === "stream" &&
        connectionRef.current &&
        connectionRef.current.readyState() === WebSocket.OPEN
      ) {
        connectionRef.current.send({
          type: "voice_end",
          session_id: sessionIdRef.current,
          conversation_id: activeConversationIdRef.current,
          voice_mode: true,
          audio_base64: capture.trailingBlob ? await blobToBase64(capture.trailingBlob) : null,
        });
        return;
      }

      await submitVoiceFallback(capture.fullBlob);
    } catch (error) {
      setSubmitting(false);
      setAssistantState("idle");
      setMicrophoneState(voiceSupported ? "idle" : "unsupported");
      setLastError(error instanceof Error ? error.message : String(error));
    }
  };

  const startNewConversation = () => {
    if (submittingRef.current || microphoneStateRef.current === "listening") {
      return;
    }

    stopSpeechPlayback();
    applyConversation(null, []);
    setStreamMode("idle");
    setLastSubmittedMessage("");
  };

  useEffect(() => {
    mountedRef.current = true;

    const bootstrap = async () => {
      try {
        const [items] = await Promise.all([listChatConversations(THREAD_LIMIT), refreshVoiceSettings()]);
        if (!mountedRef.current) {
          return;
        }

        startTransition(() => {
          setThreads(items.items);
        });

        const firstConversationId = items.items[0]?.conversation_id ?? null;
        if (firstConversationId) {
          await loadConversation(firstConversationId);
        } else {
          applyConversation(null, []);
        }
      } catch (error) {
        if (mountedRef.current) {
          setLastError(error instanceof Error ? error.message : String(error));
        }
      } finally {
        if (mountedRef.current) {
          setLoadingThreads(false);
        }
      }
    };

    bootstrap().catch(() => undefined);

    return () => {
      mountedRef.current = false;
      stopSpeechPlayback();
      void recorderRef.current?.dispose();
      connectionRef.current?.close();
      connectionRef.current = null;
      connectionPromiseRef.current = null;
    };
  }, []);

  return {
    threads,
    activeConversationId,
    messages,
    assistantDraft,
    assistantState,
    route,
    provider,
    latencyMs,
    fallbackCount,
    latestCards,
    latestTaskActions,
    lastError,
    lastSubmittedMessage,
    streamMode,
    loadingThreads,
    loadingConversation,
    submitting,
    voiceInputMode: voiceSettings.input_mode,
    showTranscriptPreview: voiceSettings.show_transcript_preview,
    voiceSupported,
    microphoneState,
    speechPlaybackState,
    transcriptPreview,
    voiceDurationMs,
    audioQueueDepth,
    async selectConversation(conversationId: string) {
      if (
        submittingRef.current ||
        microphoneStateRef.current === "listening" ||
        conversationId === activeConversationIdRef.current
      ) {
        return;
      }

      stopSpeechPlayback();
      await loadConversation(conversationId);
    },
    startNewConversation,
    submitMessage,
    retryLastTurn,
    async refreshThreads() {
      await Promise.all([refreshThreadList(), refreshVoiceSettings()]);
    },
    beginVoiceCapture,
    endVoiceCapture,
    stopSpeechPlayback,
  };
}
