import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { PageTemplate } from "@/components/PageTemplate";
import type { EmailDraftRecord, SettingsResponse } from "@/contracts/backend";
import { useEmailWorkspace } from "@/features/email/useEmailWorkspace";
import { getSettings } from "@/services/backendClient";
import styles from "./EmailPage.module.css";

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

function excerpt(value: string, limit = 140): string {
  const compact = value.replace(/\s+/g, " ").trim();
  if (!compact) {
    return "No preview available.";
  }
  return compact.length <= limit ? compact : `${compact.slice(0, limit - 1)}...`;
}

function draftStatusTone(status: string): "accent" | "sun" | "success" | "danger" {
  if (status === "sent") {
    return "success";
  }
  if (status === "draft") {
    return "accent";
  }
  return "sun";
}

function messageTone(linkedTaskCount: number): "accent" | "sun" {
  return linkedTaskCount > 0 ? "sun" : "accent";
}

function draftButtonLabel(record: EmailDraftRecord): string {
  return record.subject || record.id;
}

export function EmailPage() {
  const [settings, setSettings] = useState<SettingsResponse | null>(null);
  const [taskTitle, setTaskTitle] = useState("");
  const [taskTags, setTaskTags] = useState("");
  const [taskPriority, setTaskPriority] = useState("medium");
  const workspace = useEmailWorkspace(settings);

  useEffect(() => {
    getSettings().then(setSettings).catch(() => null);
  }, []);

  useEffect(() => {
    const selected = workspace.state.selectedMessage;
    if (!selected) {
      return;
    }
    setTaskTitle(`Follow up: ${selected.subject || selected.snippet || "email"}`);
    setTaskTags("");
    setTaskPriority("medium");
  }, [workspace.state.selectedMessage?.id]);

  const account = workspace.state.status ?? workspace.state.messages?.account ?? null;
  const messages = workspace.state.messages?.items ?? [];
  const selected = workspace.state.selectedMessage;
  const linkedTaskCount = messages.reduce((total, item) => total + item.linked_task_ids.length, 0);

  return (
    <PageTemplate
      title="Google Email"
      icon="EM"
      eyebrow="Inbox lane"
      description="A09 adds a Gmail-backed desktop workspace with connect-state handling, inbox search, local drafts, send actions, and direct email-to-task conversion."
      highlights={[
        {
          label: "Account",
          value: account?.connected ? account.email_address || "connected" : account?.status || "...",
          detail: account?.configured ? "backend OAuth ready" : "backend OAuth missing",
        },
        {
          label: "Inbox",
          value: String(workspace.state.messages?.count ?? 0),
          detail: account?.sync_enabled ? `${workspace.filters.label} search active` : "sync disabled",
        },
        {
          label: "Drafts",
          value: String(workspace.state.drafts.length),
          detail: "SQLite-backed local compose state",
        },
        {
          label: "Task links",
          value: String(linkedTaskCount),
          detail: "Messages already converted into planner work",
        },
      ]}
      actions={
        <>
          <button
            type="button"
            className="ghostButton"
            onClick={() => void workspace.refresh()}
            disabled={workspace.state.loading || workspace.state.mutating}
          >
            Refresh
          </button>
          {account?.connected ? (
            <button
              type="button"
              className="secondaryButton"
              onClick={() => void workspace.disconnectGoogle()}
              disabled={workspace.state.mutating}
            >
              Disconnect
            </button>
          ) : (
            <button
              type="button"
              className="primaryButton"
              onClick={() => void workspace.connectGoogle()}
              disabled={workspace.state.mutating || !account?.auth_url_available}
            >
              Connect Google
            </button>
          )}
        </>
      }
    >
      <div className="appStack">
        <article className={`${styles.hero} surface surfaceHero`}>
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Current implementation</span>
              <h3 className="surfaceTitle">Backend-owned Gmail state with local draft persistence</h3>
              <p className="surfaceIntro">
                Auth and provider calls stay in FastAPI, drafts stay local in SQLite, and the
                desktop shell remains explainable when Gmail is disconnected or not configured.
              </p>
            </div>
            <div className="chipRow">
              <span className="chip" data-tone={account?.connected ? "success" : "sun"}>
                {account?.status ?? "checking"}
              </span>
              <span className="chip" data-tone="accent">
                label {workspace.filters.label}
              </span>
            </div>
          </div>

          <div className={styles.heroGrid}>
            <div className={styles.heroBlock}>
              <span className="formLabel">Search Gmail</span>
              <div className={styles.searchRow}>
                <input
                  className="textInput"
                  value={workspace.filters.query}
                  onChange={(event) =>
                    workspace.setFilters((current) => ({ ...current, query: event.target.value }))
                  }
                  placeholder="invoice, from:billing@example.com, label:starred..."
                />
                <select
                  className="textInput"
                  value={workspace.filters.label}
                  onChange={(event) =>
                    workspace.setFilters((current) => ({ ...current, label: event.target.value }))
                  }
                >
                  <option value="INBOX">Inbox</option>
                  <option value="STARRED">Starred</option>
                  <option value="IMPORTANT">Important</option>
                  <option value="SENT">Sent</option>
                  <option value="ALL">All mail</option>
                </select>
                <button
                  type="button"
                  className="secondaryButton"
                  onClick={() => void workspace.refresh()}
                  disabled={workspace.state.loading || workspace.state.mutating}
                >
                  Search
                </button>
              </div>
            </div>

            <div className={styles.heroBlock}>
              <span className="formLabel">Connection notes</span>
              <p className="helperText">{account?.detail ?? "Reading Google email state."}</p>
              <p className="helperText">
                Redirect URI: <code>{account?.redirect_uri ?? "not available"}</code>
              </p>
              {workspace.state.authUrl && (
                <input className="textInput" readOnly value={workspace.state.authUrl} />
              )}
            </div>
          </div>
        </article>

        {workspace.state.loading && (
          <article className="surface">
            <span className="eyebrow">Loading</span>
            <p className="helperText">Refreshing Google email status, inbox results, and local drafts.</p>
          </article>
        )}

        {!!workspace.state.error && (
          <article className="surface">
            <span className="eyebrow">Email issue</span>
            <p className="errorText">{workspace.state.error}</p>
          </article>
        )}

        {workspace.state.mutationMessage && (
          <article className="surface surfaceMuted">
            <span className="eyebrow">Recent action</span>
            <p className="helperText">{workspace.state.mutationMessage}</p>
          </article>
        )}

        <div className={styles.workspaceGrid}>
          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Inbox results</span>
                <h3 className="surfaceTitle">Searchable message list</h3>
                <p className="surfaceIntro">
                  Gmail search runs through the backend route and keeps task-link status visible in
                  the same snapshot.
                </p>
              </div>
              <span className="chip" data-tone="accent">
                {messages.length}
              </span>
            </div>

            {messages.length ? (
              <div className={styles.messageList}>
                {messages.map((message) => (
                  <button
                    key={message.id}
                    type="button"
                    className={`${styles.messageCard} ${
                      workspace.state.selectedMessageId === message.id ? styles.messageCardActive : ""
                    }`}
                    onClick={() => void workspace.openMessage(message.id)}
                  >
                    <div className={styles.messageHeader}>
                      <div>
                        <p className={styles.messageTitle}>{message.subject}</p>
                        <p className={styles.messageMeta}>
                          {message.from_display || message.from_address} | {formatTimestamp(message.received_at)}
                        </p>
                      </div>
                      <div className="chipRow">
                        {!message.is_read && (
                          <span className="chip" data-tone="accent">
                            unread
                          </span>
                        )}
                        {message.starred && (
                          <span className="chip" data-tone="sun">
                            starred
                          </span>
                        )}
                      </div>
                    </div>
                    <p className={styles.messageSnippet}>{excerpt(message.snippet)}</p>
                    <div className="chipRow">
                      <span className="chip" data-tone={messageTone(message.linked_task_ids.length)}>
                        tasks {message.linked_task_ids.length}
                      </span>
                      {message.has_attachments && (
                        <span className="chip" data-tone="warm">
                          attachments
                        </span>
                      )}
                    </div>
                  </button>
                ))}
              </div>
            ) : (
              <div className="emptyState">
                <p className="emptyStateTitle">No messages in the current view</p>
                <p className="emptyStateText">
                  Connect Gmail, enable sync in Settings, or widen the query to pull more results.
                </p>
              </div>
            )}
          </article>

          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Message detail</span>
                <h3 className="surfaceTitle">Read context and convert to action</h3>
              </div>
              {selected && (
                <span className="chip" data-tone={selected.linked_task_ids.length > 0 ? "sun" : "accent"}>
                  linked {selected.linked_task_ids.length}
                </span>
              )}
            </div>

            {selected ? (
              <div className={styles.detailStack}>
                <div className={styles.detailBlock}>
                  <p className={styles.detailSubject}>{selected.subject}</p>
                  <p className="helperText">
                    from {selected.from_display || selected.from_address} | to {selected.to.join(", ") || "n/a"}
                  </p>
                  {selected.cc.length > 0 && (
                    <p className="helperText">cc {selected.cc.join(", ")}</p>
                  )}
                  <div className="chipRow">
                    {selected.labels.map((label) => (
                      <span key={label} className="chip" data-tone="accent">
                        {label.toLowerCase()}
                      </span>
                    ))}
                  </div>
                </div>

                <div className={`${styles.detailBlock} ${styles.bodyBlock}`}>
                  <span className="formLabel">Body</span>
                  <p className={styles.bodyText}>{selected.body_text || selected.snippet}</p>
                </div>

                <div className={styles.detailBlock}>
                  <div className="surfaceHeader">
                    <div className="surfaceHeaderBlock">
                      <span className="eyebrow">Task conversion</span>
                      <h3 className="surfaceTitle">Turn this email into planner work</h3>
                    </div>
                  </div>
                  <div className={styles.taskForm}>
                    <input
                      className="textInput"
                      value={taskTitle}
                      onChange={(event) => setTaskTitle(event.target.value)}
                      placeholder="Follow up title"
                    />
                    <div className={styles.inlineFields}>
                      <input
                        className="textInput"
                        value={taskTags}
                        onChange={(event) => setTaskTags(event.target.value)}
                        placeholder="finance, reply"
                      />
                      <select
                        className="textInput"
                        value={taskPriority}
                        onChange={(event) => setTaskPriority(event.target.value)}
                      >
                        <option value="low">Low</option>
                        <option value="medium">Medium</option>
                        <option value="high">High</option>
                        <option value="critical">Critical</option>
                      </select>
                    </div>
                    <div className="actionRow">
                      <button
                        type="button"
                        className="ghostButton"
                        onClick={() =>
                          workspace.startCreateDraft({
                            toText: selected.from_address,
                            subject: selected.subject.startsWith("Re:") ? selected.subject : `Re: ${selected.subject}`,
                            linkedMessageId: selected.id,
                          })
                        }
                      >
                        Draft reply
                      </button>
                      <button
                        type="button"
                        className="primaryButton"
                        onClick={() =>
                          void workspace.convertSelectedToTask({
                            title: taskTitle.trim() || undefined,
                            priority: taskPriority,
                            tags: taskTags
                              .split(",")
                              .map((item) => item.trim())
                              .filter((item) => item.length > 0),
                          })
                        }
                        disabled={workspace.state.mutating}
                      >
                        Convert to task
                      </button>
                    </div>
                    {selected.linked_task_ids.length > 0 && (
                      <div className="chipRow">
                        {selected.linked_task_ids.map((taskId) => (
                          <span key={taskId} className="chip" data-tone="sun">
                            task {taskId}
                          </span>
                        ))}
                        <Link className="ghostButton" to="/planner">
                          Open planner
                        </Link>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            ) : (
              <div className="emptyState">
                <p className="emptyStateTitle">Select a message to inspect it</p>
                <p className="emptyStateText">
                  The detail pane will show sender context, message text, and task-conversion controls.
                </p>
              </div>
            )}
          </article>

          <div className={styles.sideColumn}>
            <article className="surface">
              <div className="surfaceHeader">
                <div className="surfaceHeaderBlock">
                  <span className="eyebrow">Local drafts</span>
                  <h3 className="surfaceTitle">Compose and send through Gmail</h3>
                  <p className="surfaceIntro">
                    Drafts persist locally first, then send through the Gmail route once the account is connected.
                  </p>
                </div>
                <button
                  type="button"
                  className="ghostButton"
                  onClick={() => workspace.startCreateDraft()}
                  disabled={workspace.state.mutating}
                >
                  New draft
                </button>
              </div>

              {workspace.state.drafts.length ? (
                <div className="listStack">
                  {workspace.state.drafts.map((draftRecord) => (
                    <button
                      key={draftRecord.id}
                      type="button"
                      className={`${styles.draftCard} ${
                        workspace.editingDraftId === draftRecord.id ? styles.draftCardActive : ""
                      }`}
                      onClick={() => workspace.startEditDraft(draftRecord)}
                    >
                      <div>
                        <p className="listTitle">{draftButtonLabel(draftRecord)}</p>
                        <p className="listSubtitle">
                          {draftRecord.to.join(", ") || "No recipients"} | {formatTimestamp(draftRecord.updated_at)}
                        </p>
                      </div>
                      <span className="chip" data-tone={draftStatusTone(draftRecord.status)}>
                        {draftRecord.status}
                      </span>
                    </button>
                  ))}
                </div>
              ) : (
                <p className="helperText">No saved drafts yet.</p>
              )}
            </article>

            <article className="surface">
              <div className="surfaceHeader">
                <div className="surfaceHeaderBlock">
                  <span className="eyebrow">Composer</span>
                  <h3 className="surfaceTitle">
                    {workspace.draftMode === "edit" ? "Refine draft" : "Create a new draft"}
                  </h3>
                </div>
              </div>

              <div className={styles.composer}>
                <input
                  className="textInput"
                  value={workspace.draft.toText}
                  onChange={(event) =>
                    workspace.setDraft((current) => ({ ...current, toText: event.target.value }))
                  }
                  placeholder="To: person@example.com"
                />
                <input
                  className="textInput"
                  value={workspace.draft.ccText}
                  onChange={(event) =>
                    workspace.setDraft((current) => ({ ...current, ccText: event.target.value }))
                  }
                  placeholder="Cc: optional@example.com"
                />
                <input
                  className="textInput"
                  value={workspace.draft.subject}
                  onChange={(event) =>
                    workspace.setDraft((current) => ({ ...current, subject: event.target.value }))
                  }
                  placeholder="Subject"
                />
                <textarea
                  className="textArea"
                  value={workspace.draft.bodyText}
                  onChange={(event) =>
                    workspace.setDraft((current) => ({ ...current, bodyText: event.target.value }))
                  }
                  placeholder="Write the draft body here."
                />
                <div className="actionRow">
                  <button
                    type="button"
                    className="ghostButton"
                    onClick={() => workspace.startCreateDraft()}
                    disabled={workspace.state.mutating}
                  >
                    Clear
                  </button>
                  <button
                    type="button"
                    className="secondaryButton"
                    onClick={() => void workspace.submitDraft()}
                    disabled={workspace.state.mutating}
                  >
                    Save draft
                  </button>
                  <button
                    type="button"
                    className="primaryButton"
                    onClick={() => void workspace.sendCurrentDraft()}
                    disabled={workspace.state.mutating}
                  >
                    Send via Gmail
                  </button>
                </div>
              </div>
            </article>
          </div>
        </div>
      </div>
    </PageTemplate>
  );
}
