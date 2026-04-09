import { startTransition, useEffect, useState } from "react";
import type {
  BrowserAutomationRunDetail,
  BrowserAutomationRunSummary,
  BrowserAutomationTemplateRecord,
} from "@/contracts/backend";
import {
  approveBrowserAutomationRun,
  cancelBrowserAutomationRun,
  createBrowserAutomationRun,
  getBrowserAutomationRun,
  listBrowserAutomationRuns,
  listBrowserAutomationTemplates,
  rejectBrowserAutomationRun,
} from "@/services/backendClient";

export interface BrowserAutomationDraft {
  templateId: string;
  title: string;
  goal: string;
  startUrl: string;
  query: string;
  provider: string;
}

interface BrowserAutomationWorkspaceState {
  templates: BrowserAutomationTemplateRecord[];
  runs: BrowserAutomationRunSummary[];
  selectedRun: BrowserAutomationRunDetail | null;
  selectedRunId: string | null;
  loading: boolean;
  mutating: boolean;
  error: string;
  mutationMessage: string;
}

export interface BrowserAutomationWorkspace {
  state: BrowserAutomationWorkspaceState;
  draft: BrowserAutomationDraft;
  setDraft: (updater: (current: BrowserAutomationDraft) => BrowserAutomationDraft) => void;
  refresh: (selectedRunId?: string | null) => Promise<void>;
  selectRun: (runId: string) => Promise<void>;
  createRun: () => Promise<void>;
  approveSelectedRun: (approvalNote?: string) => Promise<void>;
  rejectSelectedRun: (reason: string) => Promise<void>;
  cancelSelectedRun: (reason?: string) => Promise<void>;
}

function emptyDraft(templateId = "open_page_review"): BrowserAutomationDraft {
  return {
    templateId,
    title: "",
    goal: "",
    startUrl: "https://example.com/",
    query: "",
    provider: "duckduckgo",
  };
}

function inputsForDraft(draft: BrowserAutomationDraft): Record<string, unknown> {
  if (draft.templateId === "search_query_review") {
    return {
      query: draft.query.trim(),
      provider: draft.provider.trim() || "duckduckgo",
    };
  }

  return {
    start_url: draft.startUrl.trim(),
  };
}

function mutationLabel(run: BrowserAutomationRunDetail): string {
  if (run.status === "completed") {
    return `Completed automation run ${run.title}.`;
  }
  if (run.status === "blocked") {
    return `Run ${run.title} is blocked and waiting for a recovery decision.`;
  }
  if (run.status === "cancelled") {
    return `Cancelled automation run ${run.title}.`;
  }
  return `Updated automation run ${run.title}.`;
}

export function useBrowserAutomationWorkspace(): BrowserAutomationWorkspace {
  const [state, setState] = useState<BrowserAutomationWorkspaceState>({
    templates: [],
    runs: [],
    selectedRun: null,
    selectedRunId: null,
    loading: true,
    mutating: false,
    error: "",
    mutationMessage: "",
  });
  const [draft, setDraftState] = useState<BrowserAutomationDraft>(() => emptyDraft());

  const setDraft = (updater: (current: BrowserAutomationDraft) => BrowserAutomationDraft) => {
    startTransition(() => {
      setDraftState((current) => updater(current));
    });
  };

  const refresh = async (selectedRunId?: string | null) => {
    setState((current) => ({
      ...current,
      loading: true,
      error: "",
    }));

    try {
      const [templatesResponse, runsResponse] = await Promise.all([
        listBrowserAutomationTemplates(),
        listBrowserAutomationRuns(20),
      ]);
      const resolvedSelectedId =
        selectedRunId !== undefined
          ? selectedRunId
          : state.selectedRunId && runsResponse.items.some((item) => item.id === state.selectedRunId)
            ? state.selectedRunId
            : runsResponse.items[0]?.id ?? null;
      const selectedRun = resolvedSelectedId
        ? await getBrowserAutomationRun(resolvedSelectedId).catch(() => null)
        : null;

      startTransition(() => {
        setState((current) => ({
          ...current,
          templates: templatesResponse.items,
          runs: runsResponse.items,
          selectedRun,
          selectedRunId: selectedRun?.id ?? resolvedSelectedId ?? null,
          loading: false,
          error: "",
        }));
        setDraftState((current) => {
          const templateId =
            current.templateId && templatesResponse.items.some((item) => item.template_id === current.templateId)
              ? current.templateId
              : templatesResponse.items[0]?.template_id ?? "open_page_review";
          return {
            ...current,
            templateId,
          };
        });
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

  const selectRun = async (runId: string) => {
    setState((current) => ({
      ...current,
      loading: true,
      selectedRunId: runId,
      error: "",
    }));

    try {
      const run = await getBrowserAutomationRun(runId);
      startTransition(() => {
        setState((current) => ({
          ...current,
          selectedRun: run,
          selectedRunId: run.id,
          loading: false,
          error: "",
        }));
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

  const createRun = async () => {
    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));

    try {
      const created = await createBrowserAutomationRun({
        template_id: draft.templateId,
        title: draft.title.trim(),
        goal: draft.goal.trim(),
        inputs: inputsForDraft(draft),
      });
      await refresh(created.id);
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          mutationMessage: `Created automation run ${created.title}.`,
        }));
        setDraftState((current) => ({
          ...emptyDraft(current.templateId),
          templateId: current.templateId,
          startUrl: current.startUrl,
          provider: current.provider,
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

  const approveSelectedRun = async (approvalNote = "") => {
    if (!state.selectedRunId) {
      return;
    }

    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));

    try {
      const run = await approveBrowserAutomationRun(state.selectedRunId, {
        approval_note: approvalNote.trim() || undefined,
      });
      await refresh(run.id);
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          mutationMessage: mutationLabel(run),
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

  const rejectSelectedRun = async (reason: string) => {
    if (!state.selectedRunId) {
      return;
    }

    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));

    try {
      const run = await rejectBrowserAutomationRun(state.selectedRunId, {
        reason: reason.trim(),
      });
      await refresh(run.id);
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          mutationMessage: mutationLabel(run),
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

  const cancelSelectedRun = async (reason = "") => {
    if (!state.selectedRunId) {
      return;
    }

    setState((current) => ({
      ...current,
      mutating: true,
      error: "",
      mutationMessage: "",
    }));

    try {
      const run = await cancelBrowserAutomationRun(state.selectedRunId, {
        reason: reason.trim() || undefined,
      });
      await refresh(run.id);
      startTransition(() => {
        setState((current) => ({
          ...current,
          mutating: false,
          mutationMessage: mutationLabel(run),
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
    setDraft,
    refresh,
    selectRun,
    createRun,
    approveSelectedRun,
    rejectSelectedRun,
    cancelSelectedRun,
  };
}
