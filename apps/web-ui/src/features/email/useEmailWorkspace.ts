import { startTransition, useEffect, useState } from "react";
import type {
  EmailDraftRecord,
  EmailMessageDetail,
  EmailMessageListResponse,
  GoogleEmailStatusResponse,
  SettingsResponse,
  TaskRecord,
} from "@/contracts/backend";
import {
  convertEmailMessageToTask,
  createEmailDraft,
  disconnectGoogleEmail,
  getEmailMessage,
  getGoogleEmailStatus,
  listEmailDrafts,
  listEmailMessages,
  sendEmailDraft,
  startGoogleEmailConnect,
  updateEmailDraft,
} from "@/services/backendClient";

export interface EmailDraftForm {
  toText: string;
  ccText: string;
  bccText: string;
  subject: string;
  bodyText: string;
  linkedMessageId: string;
}

export interface EmailFilters {
  query: string;
  label: string;
}

interface EmailWorkspaceState {
  status: GoogleEmailStatusResponse | null;
  messages: EmailMessageListResponse | null;
  drafts: EmailDraftRecord[];
  selectedMessage: EmailMessageDetail | null;
  selectedMessageId: string | null;
  loading: boolean;
  mutating: boolean;
  error: string;
  mutationMessage: string;
  authUrl: string;
}

export interface EmailWorkspace {
  state: EmailWorkspaceState;
  filters: EmailFilters;
  setFilters: (updater: (current: EmailFilters) => EmailFilters) => void;
  draft: EmailDraftForm;
  draftMode: "create" | "edit";
  editingDraftId: string | null;
  setDraft: (updater: (current: EmailDraftForm) => EmailDraftForm) => void;
  refresh: (nextFilters?: Partial<EmailFilters>) => Promise<void>;
  openMessage: (messageId: string) => Promise<void>;
  startCreateDraft: (preset?: Partial<EmailDraftForm>) => void;
  startEditDraft: (draft: EmailDraftRecord) => void;
  submitDraft: () => Promise<void>;
  sendCurrentDraft: () => Promise<void>;
  connectGoogle: () => Promise<void>;
  disconnectGoogle: () => Promise<void>;
  convertSelectedToTask: (payload: { title?: string; tags?: string[]; priority?: string }) => Promise<TaskRecord | null>;
}

function emptyDraft(preset: Partial<EmailDraftForm> = {}): EmailDraftForm {
  return {
    toText: "",
    ccText: "",
    bccText: "",
    subject: "",
    bodyText: "",
    linkedMessageId: "",
    ...preset,
  };
}

function recordToDraft(record: EmailDraftRecord): EmailDraftForm {
  return {
    toText: record.to.join(", "),
    ccText: record.cc.join(", "),
    bccText: record.bcc.join(", "),
    subject: record.subject,
    bodyText: record.body_text,
    linkedMessageId: record.linked_message_id ?? "",
  };
}

function parseRecipients(value: string): string[] {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter((item, index, values) => item.length > 0 && values.indexOf(item) === index);
}

export function useEmailWorkspace(settings: SettingsResponse | null): EmailWorkspace {
  const [state, setState] = useState<EmailWorkspaceState>({
    status: null,
    messages: null,
    drafts: [],
    selectedMessage: null,
    selectedMessageId: null,
    loading: true,
    mutating: false,
    error: "",
    mutationMessage: "",
    authUrl: "",
  });
  const [filters, setFiltersState] = useState<EmailFilters>({
    query: "",
    label: settings?.google_email.default_label ?? "INBOX",
  });
  const [draft, setDraftState] = useState<EmailDraftForm>(() => emptyDraft());
  const [draftMode, setDraftMode] = useState<"create" | "edit">("create");
  const [editingDraftId, setEditingDraftId] = useState<string | null>(null);

  const setFilters = (updater: (current: EmailFilters) => EmailFilters) => {
    startTransition(() => {
      setFiltersState((current) => updater(current));
    });
  };

  const setDraft = (updater: (current: EmailDraftForm) => EmailDraftForm) => {
    startTransition(() => {
      setDraftState((current) => updater(current));
    });
  };

  const openMessage = async (messageId: string) => {
    setState((current) => ({
      ...current,
      selectedMessageId: messageId,
      error: "",
    }));
    try {
      const message = await getEmailMessage(messageId);
      startTransition(() => {
        setState((current) => ({
          ...current,
          selectedMessageId: messageId,
          selectedMessage: message,
          error: "",
        }));
      });
    } catch (error) {
      startTransition(() => {
        setState((current) => ({
          ...current,
          error: error instanceof Error ? error.message : String(error),
        }));
      });
    }
  };

  const refresh = async (nextFilters: Partial<EmailFilters> = {}) => {
    const resolvedFilters = { ...filters, ...nextFilters };
    setState((current) => ({
      ...current,
      loading: true,
      error: "",
    }));

    try {
      const [status, messages, draftsResponse] = await Promise.all([
        getGoogleEmailStatus(),
        listEmailMessages({
          query: resolvedFilters.query,
          label: resolvedFilters.label,
          limit: settings?.google_email.query_limit ?? 20,
        }),
        listEmailDrafts(40),
      ]);

      const selectedMessageId =
        state.selectedMessageId && messages.items.some((item) => item.id === state.selectedMessageId)
          ? state.selectedMessageId
          : messages.items[0]?.id ?? null;
      const selectedMessage = selectedMessageId ? await getEmailMessage(selectedMessageId).catch(() => null) : null;

      startTransition(() => {
        setFiltersState(resolvedFilters);
        setState((current) => ({
          ...current,
          status,
          messages,
          drafts: draftsResponse.items,
          selectedMessageId,
          selectedMessage,
          loading: false,
          error: "",
        }));
        if (draftMode === "edit" && editingDraftId) {
          const fresh = draftsResponse.items.find((item) => item.id === editingDraftId);
          if (fresh) {
            setDraftState(recordToDraft(fresh));
          }
        }
      });
    } catch (error) {
      startTransition(() => {
        setFiltersState(resolvedFilters);
        setState((current) => ({
          ...current,
          loading: false,
          error: error instanceof Error ? error.message : String(error),
        }));
      });
    }
  };

  useEffect(() => {
    if (settings?.google_email.default_label && filters.label === "INBOX") {
      setFiltersState((current) => ({
        ...current,
        label: settings.google_email.default_label,
      }));
    }
  }, [settings?.google_email.default_label]);

  useEffect(() => {
    void refresh();
  }, []);

  const startCreateDraft = (preset: Partial<EmailDraftForm> = {}) => {
    startTransition(() => {
      setDraftMode("create");
      setEditingDraftId(null);
      setDraftState(emptyDraft(preset));
      setState((current) => ({
        ...current,
        mutationMessage: "",
      }));
    });
  };

  const startEditDraft = (record: EmailDraftRecord) => {
    startTransition(() => {
      setDraftMode("edit");
      setEditingDraftId(record.id);
      setDraftState(recordToDraft(record));
      setState((current) => ({
        ...current,
        mutationMessage: "",
      }));
    });
  };

  const submitDraft = async () => {
    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));
    try {
      const payload = {
        to: parseRecipients(draft.toText),
        cc: parseRecipients(draft.ccText),
        bcc: parseRecipients(draft.bccText),
        subject: draft.subject.trim(),
        body_text: draft.bodyText.trim(),
        linked_message_id: draft.linkedMessageId.trim() || null,
      };
      const saved =
        draftMode === "edit" && editingDraftId
          ? await updateEmailDraft(editingDraftId, payload)
          : await createEmailDraft(payload);

      await refresh();
      startTransition(() => {
        setDraftMode("edit");
        setEditingDraftId(saved.id);
        setDraftState(recordToDraft(saved));
        setState((current) => ({
          ...current,
          mutating: false,
          mutationMessage:
            draftMode === "edit" ? `Updated draft ${saved.subject || saved.id}.` : `Saved draft ${saved.subject || saved.id}.`,
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

  const sendCurrentDraft = async () => {
    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));
    try {
      let targetDraftId = editingDraftId;
      if (!targetDraftId) {
        const created = await createEmailDraft({
          to: parseRecipients(draft.toText),
          cc: parseRecipients(draft.ccText),
          bcc: parseRecipients(draft.bccText),
          subject: draft.subject.trim(),
          body_text: draft.bodyText.trim(),
          linked_message_id: draft.linkedMessageId.trim() || null,
        });
        targetDraftId = created.id;
      }
      const sent = await sendEmailDraft(targetDraftId);
      await refresh();
      startTransition(() => {
        setDraftMode("edit");
        setEditingDraftId(sent.id);
        setDraftState(recordToDraft(sent));
        setState((current) => ({
          ...current,
          mutating: false,
          mutationMessage: `Sent ${sent.subject || sent.id}.`,
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

  const connectGoogle = async () => {
    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));
    try {
      const payload = await startGoogleEmailConnect();
      window.open(payload.authorization_url, "_blank", "noopener,noreferrer");
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          authUrl: payload.authorization_url,
          mutationMessage: "Opened Google consent. Finish the browser flow, then refresh facts.",
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

  const disconnectGoogle = async () => {
    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));
    try {
      await disconnectGoogleEmail();
      await refresh();
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          selectedMessage: null,
          selectedMessageId: null,
          mutationMessage: "Disconnected the current Google email account.",
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

  const convertSelectedToTask = async (payload: {
    title?: string;
    tags?: string[];
    priority?: string;
  }): Promise<TaskRecord | null> => {
    if (!state.selectedMessageId) {
      return null;
    }
    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));
    try {
      const task = await convertEmailMessageToTask(state.selectedMessageId, payload);
      await refresh();
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          mutationMessage: `Created task ${task.title}.`,
        }));
      });
      return task;
    } catch (error) {
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          error: error instanceof Error ? error.message : String(error),
        }));
      });
      return null;
    }
  };

  return {
    state,
    filters,
    setFilters,
    draft,
    draftMode,
    editingDraftId,
    setDraft,
    refresh,
    openMessage,
    startCreateDraft,
    startEditDraft,
    submitDraft,
    sendCurrentDraft,
    connectGoogle,
    disconnectGoogle,
    convertSelectedToTask,
  };
}
