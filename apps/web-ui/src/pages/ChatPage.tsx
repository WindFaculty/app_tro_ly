import { useDeferredValue, useMemo, useState } from "react";
import { PageTemplate } from "@/components/PageTemplate";
import type {
  ChatActionReport,
  ChatConversationSummary,
  ChatMessageRecord,
} from "@/contracts/backend";
import { useAssistantWorkspace } from "@/features/chat/useAssistantWorkspace";
import styles from "./ChatPage.module.css";

const QUICK_PROMPTS = [
  "Summarize my workload for today and call out the riskiest deadlines.",
  "Find the most urgent unfinished tasks and suggest the next move.",
  "Draft a calm check-in message for tonight.",
];

function toneForAssistantState(
  state: string,
): "accent" | "sun" | "success" | "danger" | "warm" {
  switch (state) {
    case "idle":
      return "accent";
    case "thinking":
    case "waiting":
    case "listening":
      return "sun";
    case "talking":
      return "success";
    case "error":
      return "danger";
    default:
      return "warm";
  }
}

function formatTimestamp(value?: string | null): string {
  if (!value) {
    return "No timestamp";
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(parsed);
}

function matchesThread(thread: ChatConversationSummary, query: string): boolean {
  if (!query) {
    return true;
  }

  const haystack = [
    thread.last_message_preview,
    thread.summary_text,
    thread.mode,
    thread.conversation_id,
  ]
    .join(" ")
    .toLowerCase();

  return haystack.includes(query.toLowerCase());
}

function matchesMessage(message: ChatMessageRecord, query: string): boolean {
  if (!query) {
    return true;
  }

  const haystack = [
    message.role,
    message.content,
    String(message.metadata.route ?? ""),
    String(message.metadata.provider ?? ""),
  ]
    .join(" ")
    .toLowerCase();

  return haystack.includes(query.toLowerCase());
}

function describeAction(action: ChatActionReport): string {
  const title = action.title?.trim() || "Untitled task";
  const detail = action.detail?.trim();

  const base = (() => {
    switch (action.type) {
      case "create_task":
        return `Created ${title}`;
      case "complete_task":
        return `Completed ${title}`;
      case "reschedule_task":
        return `Rescheduled ${title}`;
      case "priority_task":
        return `Raised priority for ${title}`;
      default:
        return `Applied ${action.type} for ${title}`;
    }
  })();

  return detail ? `${base} - ${detail}` : base;
}

function cardTitle(card: Record<string, unknown>): string {
  return typeof card.type === "string" ? card.type.replace(/_/g, " ") : "assistant card";
}

function plannerSteps(card: Record<string, unknown>): string[] {
  const payload = card.payload;
  if (!payload || typeof payload !== "object") {
    return [];
  }

  const actionablePlan = (payload as { actionable_plan?: unknown }).actionable_plan;
  return Array.isArray(actionablePlan)
    ? actionablePlan.filter((item): item is string => typeof item === "string")
    : [];
}

function plannerSummary(card: Record<string, unknown>): string {
  const payload = card.payload;
  if (!payload || typeof payload !== "object") {
    return "No extra structured payload returned for this turn.";
  }

  const summary = (payload as { spoken_brief?: unknown; reasoning_summary?: unknown }).spoken_brief;
  if (typeof summary === "string" && summary.trim()) {
    return summary;
  }

  const reasoning = (payload as { reasoning_summary?: unknown }).reasoning_summary;
  if (typeof reasoning === "string" && reasoning.trim()) {
    return reasoning;
  }

  return "No extra structured payload returned for this turn.";
}

export function ChatPage() {
  const workspace = useAssistantWorkspace();
  const [composerValue, setComposerValue] = useState(QUICK_PROMPTS[0]);
  const [searchValue, setSearchValue] = useState("");
  const deferredSearchValue = useDeferredValue(searchValue.trim().toLowerCase());

  const filteredThreads = useMemo(
    () => workspace.threads.filter((thread) => matchesThread(thread, deferredSearchValue)),
    [deferredSearchValue, workspace.threads],
  );

  const filteredMessages = useMemo(
    () => workspace.messages.filter((message) => matchesMessage(message, deferredSearchValue)),
    [deferredSearchValue, workspace.messages],
  );

  const visibleMessages = deferredSearchValue ? filteredMessages : workspace.messages;
  const activeThread = workspace.threads.find(
    (thread) => thread.conversation_id === workspace.activeConversationId,
  );
  const actionCount = workspace.latestTaskActions.length;
  const cardCount = workspace.latestCards.length;

  const handleSubmit = async () => {
    const normalized = composerValue.trim();
    if (!normalized) {
      return;
    }

    await workspace.submitMessage(normalized);
    setComposerValue("");
  };

  const handleComposerKeyDown = async (
    event: React.KeyboardEvent<HTMLTextAreaElement>,
  ) => {
    if ((event.metaKey || event.ctrlKey) && event.key === "Enter") {
      event.preventDefault();
      await handleSubmit();
    }
  };

  return (
    <PageTemplate
      title="Chat"
      icon="CH"
      eyebrow="Assistant lane"
      description="A06 turns chat into the primary assistant surface with backend-backed threads, live streaming state, retries, local search, and structured action cards."
      actions={
        <>
          <button
            type="button"
            className="secondaryButton"
            onClick={workspace.startNewConversation}
            disabled={workspace.submitting}
          >
            New thread
          </button>
          <button
            type="button"
            className="ghostButton"
            onClick={() => void workspace.retryLastTurn()}
            disabled={!workspace.lastSubmittedMessage || workspace.submitting}
          >
            Retry
          </button>
          <button
            type="button"
            className="ghostButton"
            onClick={() => void workspace.refreshThreads()}
            disabled={workspace.loadingThreads || workspace.submitting}
          >
            Refresh
          </button>
        </>
      }
      highlights={[
        {
          label: "Threads",
          value: String(workspace.threads.length),
          detail: activeThread ? `Active ${activeThread.conversation_id}` : "Start a new thread",
        },
        {
          label: "State",
          value: workspace.assistantState || "idle",
          detail: workspace.streamMode === "fallback" ? "REST fallback active" : "Stream-first transport",
        },
        {
          label: "Route",
          value: workspace.route || "pending",
          detail: workspace.provider || "Provider pending",
        },
        {
          label: "Actions",
          value: String(actionCount + cardCount),
          detail: `${actionCount} task actions, ${cardCount} cards`,
        },
      ]}
    >
      <div className="appStack">
        <article className="surface">
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Conversation workspace</span>
              <h3 className="surfaceTitle">Recent threads and local search</h3>
              <p className="surfaceIntro">
                Search filters both the recent thread rail and the visible transcript below without
                inventing any new backend search routes.
              </p>
            </div>
            <span className="chip" data-tone={toneForAssistantState(workspace.assistantState)}>
              {workspace.assistantState || "idle"}
            </span>
          </div>

          <div className={styles.searchRow}>
            <input
              className="textInput"
              type="search"
              value={searchValue}
              onChange={(event) => setSearchValue(event.target.value)}
              placeholder="Search threads, messages, routes, or providers"
            />
            <span className="chip" data-tone={deferredSearchValue ? "warm" : "accent"}>
              {deferredSearchValue ? `${filteredMessages.length} matches` : "search ready"}
            </span>
          </div>

          <div className={styles.threadList}>
            {workspace.loadingThreads ? (
              <div className="emptyState">
                <p className="emptyStateTitle">Loading threads</p>
                <p className="emptyStateText">Reading recent conversation history from the backend.</p>
              </div>
            ) : filteredThreads.length > 0 ? (
              filteredThreads.map((thread) => (
                <button
                  key={thread.conversation_id}
                  type="button"
                  className={`${styles.threadButton} ${
                    thread.conversation_id === workspace.activeConversationId
                      ? styles.threadButtonActive
                      : ""
                  }`}
                  onClick={() => void workspace.selectConversation(thread.conversation_id)}
                  disabled={workspace.submitting}
                >
                  <div className={styles.threadTopline}>
                    <span className={styles.threadMode}>{thread.mode}</span>
                    <span className={styles.threadTime}>{formatTimestamp(thread.updated_at)}</span>
                  </div>
                  <p className={styles.threadPreview}>
                    {thread.summary_text?.trim() || thread.last_message_preview || "No preview yet"}
                  </p>
                  <div className="chipRow">
                    <span className="chip" data-tone="accent">
                      {thread.message_count} messages
                    </span>
                    {thread.last_message_role && (
                      <span className="chip" data-tone="sun">
                        last {thread.last_message_role}
                      </span>
                    )}
                  </div>
                </button>
              ))
            ) : (
              <div className="emptyState">
                <p className="emptyStateTitle">No matching threads</p>
                <p className="emptyStateText">
                  {workspace.threads.length === 0
                    ? "Send the first turn to create a persisted conversation."
                    : "Adjust the search text or start a new thread."}
                </p>
              </div>
            )}
          </div>
        </article>

        <article className="surface surfaceHero">
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Transcript</span>
              <h3 className="surfaceTitle">Stream-first conversation lane</h3>
              <p className="surfaceIntro">
                Text turns prefer `WS /v1/assistant/stream` and fall back to `POST /v1/chat` only
                when the stream cannot start.
              </p>
            </div>
            <div className="chipRow">
              <span className="chip" data-tone={toneForAssistantState(workspace.assistantState)}>
                {workspace.assistantState || "idle"}
              </span>
              <span className="chip" data-tone={workspace.streamMode === "fallback" ? "sun" : "success"}>
                {workspace.streamMode === "fallback" ? "rest fallback" : "stream live"}
              </span>
            </div>
          </div>

          <div className={styles.transcriptPanel}>
            <div className={styles.transcriptScroller}>
              {workspace.loadingConversation ? (
                <div className="emptyState">
                  <p className="emptyStateTitle">Loading transcript</p>
                  <p className="emptyStateText">Pulling the selected conversation from the backend.</p>
                </div>
              ) : visibleMessages.length > 0 ? (
                visibleMessages.map((message) => (
                  <article
                    key={message.id}
                    className={`${styles.messageCard} ${
                      message.role === "user" ? styles.messageUser : styles.messageAssistant
                    }`}
                  >
                    <div className={styles.messageHeader}>
                      <span className={styles.messageRole}>{message.role === "user" ? "Me" : "Assistant"}</span>
                      <span className={styles.messageTime}>{formatTimestamp(message.created_at)}</span>
                    </div>
                    <p className={styles.messageBody}>{message.content}</p>
                    <div className="chipRow">
                      {typeof message.metadata.route === "string" && message.metadata.route && (
                        <span className="chip" data-tone="accent">
                          route {message.metadata.route}
                        </span>
                      )}
                      {typeof message.metadata.provider === "string" && message.metadata.provider && (
                        <span className="chip" data-tone="warm">
                          provider {message.metadata.provider}
                        </span>
                      )}
                    </div>
                  </article>
                ))
              ) : (
                <div className="emptyState">
                  <p className="emptyStateTitle">No visible transcript yet</p>
                  <p className="emptyStateText">
                    {deferredSearchValue
                      ? "The current search query does not match any message in this thread."
                      : "Start a new turn or select an existing conversation thread."}
                  </p>
                </div>
              )}

              {workspace.assistantDraft && (
                <article className={`${styles.messageCard} ${styles.messageAssistant} ${styles.messageDraft}`}>
                  <div className={styles.messageHeader}>
                    <span className={styles.messageRole}>Assistant draft</span>
                    <span className={styles.messageTime}>streaming</span>
                  </div>
                  <p className={styles.messageBody}>{workspace.assistantDraft}</p>
                </article>
              )}
            </div>

            <div className={styles.composerPanel}>
              <div className="chipRow">
                {QUICK_PROMPTS.map((prompt) => (
                  <button
                    key={prompt}
                    type="button"
                    className="ghostButton"
                    onClick={() => setComposerValue(prompt)}
                    disabled={workspace.submitting}
                  >
                    {prompt}
                  </button>
                ))}
              </div>

              <label className="formLabel" htmlFor="chat-message">
                Prompt
              </label>
              <textarea
                id="chat-message"
                className="textArea"
                rows={6}
                value={composerValue}
                onChange={(event) => setComposerValue(event.target.value)}
                onKeyDown={(event) => void handleComposerKeyDown(event)}
                placeholder="Ask about tasks, planning, or concrete next steps"
              />
              <div className="actionRow">
                <span className="helperText">
                  Send with Ctrl+Enter. Voice transport stays out of scope here until A12.
                </span>
                <button
                  type="button"
                  className="primaryButton"
                  onClick={() => void handleSubmit()}
                  disabled={workspace.submitting || !composerValue.trim()}
                >
                  {workspace.submitting ? "Sending..." : "Send turn"}
                </button>
              </div>
            </div>
          </div>
        </article>

        <div className="surfaceGrid">
          <article className="surface surfaceMuted">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Diagnostics</span>
                <h3 className="surfaceTitle">Routing and fallback signal</h3>
              </div>
            </div>

            <div className="detailGrid">
              <div className="detailRow">
                <span className="detailLabel">Conversation</span>
                <span className="detailValue">{workspace.activeConversationId ?? "new thread"}</span>
              </div>
              <div className="detailRow">
                <span className="detailLabel">Route</span>
                <span className="detailValue">{workspace.route || "pending"}</span>
              </div>
              <div className="detailRow">
                <span className="detailLabel">Provider</span>
                <span className="detailValue">{workspace.provider || "pending"}</span>
              </div>
              <div className="detailRow">
                <span className="detailLabel">Latency</span>
                <span className="detailValue">
                  {workspace.latencyMs === null ? "pending" : `${workspace.latencyMs} ms`}
                </span>
              </div>
              <div className="detailRow">
                <span className="detailLabel">Fallbacks</span>
                <span className="detailValue">{workspace.fallbackCount}</span>
              </div>
            </div>
          </article>

          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Action confirmation</span>
                <h3 className="surfaceTitle">Validated backend actions</h3>
              </div>
              <span className="chip" data-tone={actionCount > 0 ? "success" : "accent"}>
                {actionCount} actions
              </span>
            </div>

            {actionCount > 0 ? (
              <div className="listStack">
                {workspace.latestTaskActions.map((action, index) => (
                  <div key={`${action.type}-${index}`} className="listRow">
                    <div>
                      <p className="listTitle">{action.type}</p>
                      <p className="listSubtitle">{describeAction(action)}</p>
                    </div>
                    <span className="chip" data-tone={action.status === "done" ? "success" : "accent"}>
                      {action.status}
                    </span>
                  </div>
                ))}
              </div>
            ) : (
              <div className="emptyState">
                <p className="emptyStateTitle">No task actions yet</p>
                <p className="emptyStateText">
                  Validated task changes returned by the assistant will appear here after a turn.
                </p>
              </div>
            )}
          </article>
        </div>

        <article className="surface surfaceMuted">
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Assistant cards</span>
              <h3 className="surfaceTitle">Structured plan and reasoning payloads</h3>
            </div>
            <span className="chip" data-tone={cardCount > 0 ? "warm" : "accent"}>
              {cardCount} cards
            </span>
          </div>

          {cardCount > 0 ? (
            <div className="listStack">
              {workspace.latestCards.map((card, index) => (
                <article key={`${card.type ?? "card"}-${index}`} className={styles.cardPanel}>
                  <div className={styles.cardHeader}>
                    <p className="listTitle">{cardTitle(card)}</p>
                    <span className="chip" data-tone="warm">
                      card {index + 1}
                    </span>
                  </div>
                  <p className="listSubtitle">{plannerSummary(card)}</p>
                  {plannerSteps(card).length > 0 && (
                    <div className={styles.cardSteps}>
                      {plannerSteps(card).map((step) => (
                        <div key={step} className={styles.cardStep}>
                          {step}
                        </div>
                      ))}
                    </div>
                  )}
                </article>
              ))}
            </div>
          ) : (
            <div className="emptyState">
              <p className="emptyStateTitle">No structured cards yet</p>
              <p className="emptyStateText">
                Deeper planning payloads such as `planner_output` land here when the selected route
                returns them.
              </p>
            </div>
          )}
        </article>

        {workspace.lastError && (
          <article className="surface">
            <span className="eyebrow">Assistant issue</span>
            <p className="errorText">{workspace.lastError}</p>
          </article>
        )}
      </div>
    </PageTemplate>
  );
}
