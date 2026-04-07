import { startTransition, useEffect, useRef, useState } from "react";
import type {
  AssistantFinalEvent,
  AssistantStreamEvent,
  ChatActionReport,
  ChatConversationSummary,
  ChatMessageRecord,
  ChatResponse,
} from "@/contracts/backend";
import { getChatConversation, listChatConversations, sendChatMessage } from "@/services/backendClient";
import {
  connectAssistantStream,
  type AssistantStreamConnection,
} from "@/services/assistantStream";

const THREAD_LIMIT = 24;

interface ConversationSignals {
  route: string;
  provider: string;
  latencyMs: number | null;
  fallbackCount: number;
  latestCards: Array<Record<string, unknown>>;
  latestTaskActions: ChatActionReport[];
}

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
  streamMode: "idle" | "stream" | "fallback";
  loadingThreads: boolean;
  loadingConversation: boolean;
  submitting: boolean;
}

export interface AssistantWorkspaceApi extends AssistantWorkspaceState {
  selectConversation: (conversationId: string) => Promise<void>;
  startNewConversation: () => void;
  submitMessage: (message: string) => Promise<void>;
  retryLastTurn: () => Promise<void>;
  refreshThreads: () => Promise<void>;
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

export function useAssistantWorkspace(): AssistantWorkspaceApi {
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
  const [streamMode, setStreamMode] = useState<"idle" | "stream" | "fallback">("idle");
  const [loadingThreads, setLoadingThreads] = useState(true);
  const [loadingConversation, setLoadingConversation] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const mountedRef = useRef(true);
  const connectionRef = useRef<AssistantStreamConnection | null>(null);
  const connectionPromiseRef = useRef<Promise<AssistantStreamConnection> | null>(null);
  const sessionIdRef = useRef(createUiId("sess"));
  const activeConversationIdRef = useRef<string | null>(null);
  const lastSubmittedMessageRef = useRef("");
  const submittingRef = useRef(false);

  useEffect(() => {
    activeConversationIdRef.current = activeConversationId;
  }, [activeConversationId]);

  useEffect(() => {
    lastSubmittedMessageRef.current = lastSubmittedMessage;
  }, [lastSubmittedMessage]);

  useEffect(() => {
    submittingRef.current = submitting;
  }, [submitting]);

  const syncSignalsFromMessages = (nextMessages: ChatMessageRecord[]) => {
    const signals = extractConversationSignals(nextMessages);
    setRoute(signals.route);
    setProvider(signals.provider);
    setLatencyMs(signals.latencyMs);
    setFallbackCount(signals.fallbackCount);
    setLatestCards(signals.latestCards);
    setLatestTaskActions(signals.latestTaskActions);
  };

  const applyConversation = (conversationId: string | null, nextMessages: ChatMessageRecord[]) => {
    startTransition(() => {
      setActiveConversationId(conversationId);
      setMessages(nextMessages);
      setAssistantDraft("");
      setAssistantState("idle");
      setLastError("");
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
      case "assistant_chunk":
        setAssistantDraft((current) =>
          current ? `${current} ${event.text.trim()}`.trim() : event.text.trim(),
        );
        break;
      case "task_action_applied":
        setLatestTaskActions((current) => [...current, event.action].slice(-6));
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
        });
        void refreshThreadList(event.conversation_id);
        break;
      }
      case "error":
        setSubmitting(false);
        setAssistantState("idle");
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

  const submitViaRest = async (message: string) => {
    const response = await sendChatMessage({
      message,
      conversation_id: activeConversationIdRef.current,
      session_id: sessionIdRef.current,
      mode: "text",
      include_voice: false,
      voice_mode: false,
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
    });

    await refreshThreadList(response.conversation_id);
  };

  const submitMessage = async (message: string) => {
    const normalizedMessage = message.trim();
    if (!normalizedMessage || submittingRef.current) {
      return;
    }

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
      await submitViaRest(normalizedMessage);
    }
  };

  const retryLastTurn = async () => {
    if (!lastSubmittedMessageRef.current || submittingRef.current) {
      return;
    }

    await submitMessage(lastSubmittedMessageRef.current);
  };

  const startNewConversation = () => {
    if (submittingRef.current) {
      return;
    }

    applyConversation(null, []);
    setStreamMode("idle");
    setLastSubmittedMessage("");
  };

  useEffect(() => {
    mountedRef.current = true;

    const bootstrap = async () => {
      try {
        const items = await listChatConversations(THREAD_LIMIT);
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
    async selectConversation(conversationId: string) {
      if (submittingRef.current || conversationId === activeConversationIdRef.current) {
        return;
      }
      await loadConversation(conversationId);
    },
    startNewConversation,
    submitMessage,
    retryLastTurn,
    async refreshThreads() {
      await refreshThreadList();
    },
  };
}
