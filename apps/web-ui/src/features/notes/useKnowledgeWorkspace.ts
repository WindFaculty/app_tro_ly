import { startTransition, useEffect, useState } from "react";
import type {
  ChatConversationListResponse,
  MemoryListResponse,
  NoteCreatePayload,
  NoteListResponse,
  NoteRecord,
  TaskListResponse,
} from "@/contracts/backend";
import {
  createNote,
  getActiveTasks,
  getMemoryItems,
  listChatConversations,
  listNotes,
  updateNote,
} from "@/services/backendClient";

export interface NoteDraft {
  title: string;
  body: string;
  tagsText: string;
  linkedTaskId: string;
  linkedConversationId: string;
  pinned: boolean;
}

interface KnowledgeWorkspaceState {
  notes: NoteListResponse | null;
  memory: MemoryListResponse | null;
  tasks: TaskListResponse | null;
  conversations: ChatConversationListResponse | null;
  loading: boolean;
  mutating: boolean;
  error: string;
  mutationMessage: string;
}

export interface KnowledgeWorkspace {
  state: KnowledgeWorkspaceState;
  draft: NoteDraft;
  draftMode: "create" | "edit";
  editingNoteId: string | null;
  setDraft: (updater: (current: NoteDraft) => NoteDraft) => void;
  startCreate: (preset?: Partial<NoteDraft>) => void;
  startEdit: (note: NoteRecord) => void;
  submitDraft: () => Promise<void>;
  refresh: () => Promise<void>;
}

function emptyDraft(preset: Partial<NoteDraft> = {}): NoteDraft {
  return {
    title: "",
    body: "",
    tagsText: "",
    linkedTaskId: "",
    linkedConversationId: "",
    pinned: false,
    ...preset,
  };
}

function noteToDraft(note: NoteRecord): NoteDraft {
  return {
    title: note.title,
    body: note.body,
    tagsText: note.tags.join(", "),
    linkedTaskId: note.linked_task_id ?? "",
    linkedConversationId: note.linked_conversation_id ?? "",
    pinned: note.pinned,
  };
}

function parseTags(value: string): string[] {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter((item, index, values) => item.length > 0 && values.indexOf(item) === index);
}

function draftToPayload(draft: NoteDraft): NoteCreatePayload {
  return {
    title: draft.title.trim(),
    body: draft.body.trim(),
    tags: parseTags(draft.tagsText),
    linked_task_id: draft.linkedTaskId.trim() || null,
    linked_conversation_id: draft.linkedConversationId.trim() || null,
    pinned: draft.pinned,
  };
}

export function useKnowledgeWorkspace(): KnowledgeWorkspace {
  const [state, setState] = useState<KnowledgeWorkspaceState>({
    notes: null,
    memory: null,
    tasks: null,
    conversations: null,
    loading: true,
    mutating: false,
    error: "",
    mutationMessage: "",
  });
  const [draft, setDraftState] = useState<NoteDraft>(() => emptyDraft());
  const [draftMode, setDraftMode] = useState<"create" | "edit">("create");
  const [editingNoteId, setEditingNoteId] = useState<string | null>(null);

  const setDraft = (updater: (current: NoteDraft) => NoteDraft) => {
    startTransition(() => {
      setDraftState((current) => updater(current));
    });
  };

  const refresh = async () => {
    try {
      const [notes, memory, tasks, conversations] = await Promise.all([
        listNotes(),
        getMemoryItems(),
        getActiveTasks(),
        listChatConversations(24),
      ]);

      startTransition(() => {
        setState((current) => ({
          ...current,
          notes,
          memory,
          tasks,
          conversations,
          loading: false,
          error: "",
        }));

        if (editingNoteId) {
          const fresh = notes.items.find((note) => note.id === editingNoteId);
          if (fresh) {
            setDraftState(noteToDraft(fresh));
          } else {
            setDraftMode("create");
            setEditingNoteId(null);
            setDraftState(emptyDraft());
          }
        } else if (!draft.title && notes.items[0]) {
          const first = notes.items[0];
          setDraftMode("edit");
          setEditingNoteId(first.id);
          setDraftState(noteToDraft(first));
        }
      });
    } catch (error) {
      startTransition(() => {
        setState((current) => ({
          ...current,
          loading: false,
          error: error instanceof Error ? error.message : String(error),
        }));
      });
    }
  };

  useEffect(() => {
    void refresh();
  }, []);

  const startCreate = (preset: Partial<NoteDraft> = {}) => {
    startTransition(() => {
      setDraftMode("create");
      setEditingNoteId(null);
      setDraftState(emptyDraft(preset));
      setState((current) => ({
        ...current,
        mutationMessage: "",
      }));
    });
  };

  const startEdit = (note: NoteRecord) => {
    startTransition(() => {
      setDraftMode("edit");
      setEditingNoteId(note.id);
      setDraftState(noteToDraft(note));
      setState((current) => ({
        ...current,
        mutationMessage: "",
      }));
    });
  };

  const submitDraft = async () => {
    const normalizedTitle = draft.title.trim();
    if (!normalizedTitle) {
      setState((current) => ({
        ...current,
        error: "Note title must not be empty.",
      }));
      return;
    }

    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));

    try {
      const payload = draftToPayload(draft);
      const saved =
        draftMode === "edit" && editingNoteId
          ? await updateNote(editingNoteId, payload)
          : await createNote(payload);

      await refresh();

      startTransition(() => {
        setDraftMode("edit");
        setEditingNoteId(saved.id);
        setDraftState(noteToDraft(saved));
        setState((current) => ({
          ...current,
          mutating: false,
          error: "",
          mutationMessage:
            draftMode === "edit"
              ? `Updated ${saved.title}.`
              : `Captured ${saved.title}.`,
        }));
      });
    } catch (error) {
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          error: error instanceof Error ? error.message : String(error),
        }));
      });
    }
  };

  return {
    state,
    draft,
    draftMode,
    editingNoteId,
    setDraft,
    startCreate,
    startEdit,
    submitDraft,
    refresh,
  };
}
