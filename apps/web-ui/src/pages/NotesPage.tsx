import { useDeferredValue, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { PageTemplate } from "@/components/PageTemplate";
import type {
  ChatConversationSummary,
  MemoryItemRecord,
  NoteRecord,
  TaskRecord,
} from "@/contracts/backend";
import { useKnowledgeWorkspace } from "@/features/notes/useKnowledgeWorkspace";
import styles from "./NotesPage.module.css";

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
    return "No extra note detail yet.";
  }
  return compact.length <= limit ? compact : `${compact.slice(0, limit - 1)}…`;
}

function matchesNote(note: NoteRecord, query: string, tag: string): boolean {
  const haystack = [
    note.title,
    note.body,
    note.tags.join(" "),
    note.linked_task_id,
    note.linked_conversation_id,
  ]
    .join(" ")
    .toLowerCase();
  const searchMatch = !query || haystack.includes(query);
  const tagMatch = !tag || note.tags.includes(tag);
  return searchMatch && tagMatch;
}

function matchesTask(task: TaskRecord, query: string): boolean {
  if (!query) {
    return true;
  }

  const haystack = [
    task.title,
    task.description ?? "",
    task.priority,
    task.status,
    task.tags.join(" "),
  ]
    .join(" ")
    .toLowerCase();

  return haystack.includes(query);
}

function matchesConversation(thread: ChatConversationSummary, query: string): boolean {
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

  return haystack.includes(query);
}

function matchesMemory(item: MemoryItemRecord, query: string): boolean {
  if (!query) {
    return true;
  }

  const haystack = [item.category, item.content, item.status].join(" ").toLowerCase();
  return haystack.includes(query);
}

export function NotesPage() {
  const workspace = useKnowledgeWorkspace();
  const [searchValue, setSearchValue] = useState("");
  const [tagFilter, setTagFilter] = useState("");
  const deferredSearch = useDeferredValue(searchValue.trim().toLowerCase());

  const notes = workspace.state.notes?.items ?? [];
  const tasks = workspace.state.tasks?.items ?? [];
  const conversations = workspace.state.conversations?.items ?? [];
  const memoryItems = workspace.state.memory?.items ?? [];

  const filteredNotes = useMemo(
    () => notes.filter((note) => matchesNote(note, deferredSearch, tagFilter)),
    [deferredSearch, notes, tagFilter],
  );
  const filteredTasks = useMemo(
    () => tasks.filter((task) => matchesTask(task, deferredSearch)).slice(0, 6),
    [deferredSearch, tasks],
  );
  const filteredConversations = useMemo(
    () =>
      conversations
        .filter((thread) => matchesConversation(thread, deferredSearch))
        .slice(0, 6),
    [conversations, deferredSearch],
  );
  const filteredMemory = useMemo(
    () => memoryItems.filter((item) => matchesMemory(item, deferredSearch)).slice(0, 8),
    [deferredSearch, memoryItems],
  );

  const tagOptions = useMemo(
    () =>
      Array.from(new Set(notes.flatMap((note) => note.tags)))
        .sort((left, right) => left.localeCompare(right)),
    [notes],
  );

  const linkedNotesCount = notes.filter(
    (note) => note.linked_task_id || note.linked_conversation_id,
  ).length;
  const selectedTask = tasks.find((task) => task.id === workspace.draft.linkedTaskId) ?? null;
  const selectedConversation =
    conversations.find(
      (thread) => thread.conversation_id === workspace.draft.linkedConversationId,
    ) ?? null;

  return (
    <PageTemplate
      title="Notes & Knowledge"
      icon="NK"
      eyebrow="Memory lane"
      description="A08 turns notes into a real desktop module with quick capture, richer detail, links to tasks or chat, and local search across notes, active work, recent threads, and stored memory items."
      highlights={[
        {
          label: "Notes",
          value: String(workspace.state.notes?.count ?? 0),
          detail: `${linkedNotesCount} linked to task or chat context`,
        },
        {
          label: "Knowledge",
          value: String(workspace.state.memory?.count ?? 0),
          detail: "Read-only memory facts from backend extraction",
        },
        {
          label: "Search",
          value: deferredSearch ? "active" : "ready",
          detail: deferredSearch
            ? `${filteredNotes.length + filteredTasks.length + filteredConversations.length + filteredMemory.length} visible matches`
            : "Local cross-module search without inventing new backend search routes",
        },
      ]}
      actions={
        <>
          <button
            type="button"
            className="secondaryButton"
            onClick={() => workspace.startCreate()}
            disabled={workspace.state.mutating}
          >
            New note
          </button>
          <button
            type="button"
            className="ghostButton"
            onClick={() => void workspace.refresh()}
            disabled={workspace.state.loading || workspace.state.mutating}
          >
            Refresh
          </button>
        </>
      }
    >
      <div className="appStack">
        <article className={`${styles.searchPanel} surface surfaceHero`}>
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Search lens</span>
              <h3 className="surfaceTitle">Find notes, active tasks, threads, and memory signals</h3>
              <p className="surfaceIntro">
                The notes module keeps search local to the already-loaded desktop snapshot so the
                UI stays fast and explainable.
              </p>
            </div>
            <div className="chipRow">
              <span className="chip" data-tone="accent">
                notes {filteredNotes.length}
              </span>
              <span className="chip" data-tone="sun">
                tasks {filteredTasks.length}
              </span>
              <span className="chip" data-tone="warm">
                threads {filteredConversations.length}
              </span>
              <span className="chip" data-tone="success">
                memory {filteredMemory.length}
              </span>
            </div>
          </div>

          <div className={styles.searchControls}>
            <label className={styles.searchField}>
              <span className="formLabel">Cross-module search</span>
              <input
                className="textInput"
                type="search"
                value={searchValue}
                onChange={(event) => setSearchValue(event.target.value)}
                placeholder="Search note text, tags, task titles, thread summaries, or memory facts"
              />
            </label>

            <div className={styles.tagFilters}>
              <button
                type="button"
                className={`${styles.tagButton} ${tagFilter ? "" : styles.tagButtonActive}`}
                onClick={() => setTagFilter("")}
              >
                all tags
              </button>
              {tagOptions.map((tag) => (
                <button
                  key={tag}
                  type="button"
                  className={`${styles.tagButton} ${tagFilter === tag ? styles.tagButtonActive : ""}`}
                  onClick={() => setTagFilter((current) => (current === tag ? "" : tag))}
                >
                  {tag}
                </button>
              ))}
            </div>
          </div>
        </article>

        {workspace.state.loading && (
          <article className="surface">
            <span className="eyebrow">Loading</span>
            <p className="helperText">
              Refreshing notes, active tasks, recent threads, and memory items.
            </p>
          </article>
        )}

        {!!workspace.state.error && !workspace.state.mutating && (
          <article className="surface">
            <span className="eyebrow">Data issue</span>
            <p className="errorText">{workspace.state.error}</p>
          </article>
        )}

        <div className={styles.workspaceGrid}>
          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Library</span>
                <h3 className="surfaceTitle">Captured notes</h3>
                <p className="surfaceIntro">
                  Quick notes and richer note detail live in one list, with pinned items held above
                  the rest.
                </p>
              </div>
              <span className="chip" data-tone="accent">
                {filteredNotes.length}
              </span>
            </div>

            {filteredNotes.length ? (
              <div className={styles.noteList}>
                {filteredNotes.map((note) => (
                  <button
                    key={note.id}
                    type="button"
                    className={`${styles.noteCard} ${
                      workspace.editingNoteId === note.id ? styles.noteCardActive : ""
                    }`}
                    onClick={() => workspace.startEdit(note)}
                  >
                    <div className={styles.noteCardHeader}>
                      <div>
                        <p className={styles.noteTitle}>{note.title}</p>
                        <p className={styles.noteMeta}>updated {formatTimestamp(note.updated_at)}</p>
                      </div>
                      {note.pinned && (
                        <span className="chip" data-tone="sun">
                          pinned
                        </span>
                      )}
                    </div>
                    <p className={styles.noteBody}>{excerpt(note.body)}</p>
                    <div className="chipRow">
                      {note.tags.slice(0, 3).map((tag) => (
                        <span key={tag} className="chip" data-tone="accent">
                          {tag}
                        </span>
                      ))}
                      {note.linked_task_id && (
                        <span className="chip" data-tone="warm">
                          task link
                        </span>
                      )}
                      {note.linked_conversation_id && (
                        <span className="chip" data-tone="success">
                          chat link
                        </span>
                      )}
                    </div>
                  </button>
                ))}
              </div>
            ) : (
              <div className="emptyState">
                <p className="emptyStateTitle">No notes match the current filters</p>
                <p className="emptyStateText">
                  Capture a new note or relax the search and tag filters to see more history.
                </p>
              </div>
            )}
          </article>

          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Editor</span>
                <h3 className="surfaceTitle">
                  {workspace.draftMode === "edit" ? "Refine captured context" : "Capture a new note"}
                </h3>
                <p className="surfaceIntro">
                  Notes can stay lightweight or carry richer detail plus direct links to current
                  tasks and chat threads.
                </p>
              </div>
              <span
                className="chip"
                data-tone={workspace.draftMode === "edit" ? "warm" : "accent"}
              >
                {workspace.draftMode}
              </span>
            </div>

            <div className={styles.editorForm}>
              <label className={styles.fieldBlock}>
                <span className="formLabel">Title</span>
                <input
                  className="textInput"
                  value={workspace.draft.title}
                  onChange={(event) =>
                    workspace.setDraft((current) => ({
                      ...current,
                      title: event.target.value,
                    }))
                  }
                  placeholder="Workshop prep, reading notes, project risks..."
                />
              </label>

              <label className={styles.fieldBlock}>
                <span className="formLabel">Body</span>
                <textarea
                  className="textArea"
                  value={workspace.draft.body}
                  onChange={(event) =>
                    workspace.setDraft((current) => ({
                      ...current,
                      body: event.target.value,
                    }))
                  }
                  placeholder="Capture detail, references, or a follow-up checklist here."
                />
              </label>

              <div className={styles.fieldGrid}>
                <label className={styles.fieldBlock}>
                  <span className="formLabel">Tags</span>
                  <input
                    className="textInput"
                    value={workspace.draft.tagsText}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({
                        ...current,
                        tagsText: event.target.value,
                      }))
                    }
                    placeholder="research, journal, travel"
                  />
                </label>

                <label className={styles.fieldBlock}>
                  <span className="formLabel">Linked task</span>
                  <select
                    className="textInput"
                    value={workspace.draft.linkedTaskId}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({
                        ...current,
                        linkedTaskId: event.target.value,
                      }))
                    }
                  >
                    <option value="">No task link</option>
                    {tasks.map((task) => (
                      <option key={task.id} value={task.id}>
                        {task.title}
                      </option>
                    ))}
                  </select>
                </label>

                <label className={styles.fieldBlock}>
                  <span className="formLabel">Linked thread</span>
                  <select
                    className="textInput"
                    value={workspace.draft.linkedConversationId}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({
                        ...current,
                        linkedConversationId: event.target.value,
                      }))
                    }
                  >
                    <option value="">No chat link</option>
                    {conversations.map((thread) => (
                      <option key={thread.conversation_id} value={thread.conversation_id}>
                        {thread.last_message_preview || thread.conversation_id}
                      </option>
                    ))}
                  </select>
                </label>

                <label className={styles.checkboxField}>
                  <input
                    type="checkbox"
                    checked={workspace.draft.pinned}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({
                        ...current,
                        pinned: event.target.checked,
                      }))
                    }
                  />
                  <span>
                    <strong>Pin this note</strong>
                    <span className="helperText">Pinned notes stay at the top of the library.</span>
                  </span>
                </label>
              </div>

              <div className="actionRow">
                <div className="chipRow">
                  {selectedTask && (
                    <span className="chip" data-tone="warm">
                      task: {selectedTask.title}
                    </span>
                  )}
                  {selectedConversation && (
                    <span className="chip" data-tone="success">
                      chat:{" "}
                      {selectedConversation.last_message_preview ||
                        selectedConversation.conversation_id}
                    </span>
                  )}
                </div>

                <div className="chipRow">
                  <button
                    type="button"
                    className="ghostButton"
                    onClick={() => workspace.startCreate()}
                    disabled={workspace.state.mutating}
                  >
                    Clear
                  </button>
                  <button
                    type="button"
                    className="primaryButton"
                    onClick={() => void workspace.submitDraft()}
                    disabled={workspace.state.mutating}
                  >
                    {workspace.state.mutating
                      ? "Saving..."
                      : workspace.draftMode === "edit"
                        ? "Save note"
                        : "Capture note"}
                  </button>
                </div>
              </div>

              {workspace.state.mutationMessage && (
                <p className="helperText">{workspace.state.mutationMessage}</p>
              )}
              {workspace.state.error && <p className="errorText">{workspace.state.error}</p>}
            </div>
          </article>

          <div className={styles.sideColumn}>
            <article className="surface">
              <div className="surfaceHeader">
                <div className="surfaceHeaderBlock">
                  <span className="eyebrow">Personal knowledge</span>
                  <h3 className="surfaceTitle">Stored memory signals</h3>
                  <p className="surfaceIntro">
                    These items come from backend memory extraction and stay read-only inside the
                    notes lane.
                  </p>
                </div>
                <span className="chip" data-tone="success">
                  {filteredMemory.length}
                </span>
              </div>

              {filteredMemory.length ? (
                <div className="listStack">
                  {filteredMemory.map((item) => (
                    <div key={item.id} className="listRow">
                      <div>
                        <p className="listTitle">{item.content}</p>
                        <p className="listSubtitle">
                          {item.category} | confidence {Math.round(item.confidence * 100)}%
                        </p>
                      </div>
                      <span className="chip" data-tone="success">
                        memory
                      </span>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="emptyState">
                  <p className="emptyStateTitle">No knowledge items match yet</p>
                  <p className="emptyStateText">
                    Memory extraction will surface here after assistant turns store durable facts.
                  </p>
                </div>
              )}
            </article>

            <article className="surface">
              <div className="surfaceHeader">
                <div className="surfaceHeaderBlock">
                  <span className="eyebrow">Search spillover</span>
                  <h3 className="surfaceTitle">Related work and recent threads</h3>
                </div>
              </div>

              <div className={styles.resultColumns}>
                <div className={styles.resultColumn}>
                  <span className="eyebrow">Tasks</span>
                  {filteredTasks.length ? (
                    <div className="listStack">
                      {filteredTasks.map((task) => (
                        <div key={task.id} className="listRow">
                          <div>
                            <p className="listTitle">{task.title}</p>
                            <p className="listSubtitle">
                              {task.priority} | {task.status}
                            </p>
                          </div>
                          <Link className="ghostButton" to="/planner">
                            Open planner
                          </Link>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="helperText">No active task matches the current search.</p>
                  )}
                </div>

                <div className={styles.resultColumn}>
                  <span className="eyebrow">Threads</span>
                  {filteredConversations.length ? (
                    <div className="listStack">
                      {filteredConversations.map((thread) => (
                        <div key={thread.conversation_id} className="listRow">
                          <div>
                            <p className="listTitle">
                              {thread.last_message_preview || thread.conversation_id}
                            </p>
                            <p className="listSubtitle">
                              {thread.summary_text || formatTimestamp(thread.updated_at)}
                            </p>
                          </div>
                          <Link className="ghostButton" to="/chat">
                            Open chat
                          </Link>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <p className="helperText">No recent thread matches the current search.</p>
                  )}
                </div>
              </div>
            </article>
          </div>
        </div>
      </div>
    </PageTemplate>
  );
}
