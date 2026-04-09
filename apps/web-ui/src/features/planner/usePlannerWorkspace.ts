import { startTransition, useEffect, useMemo, useState } from "react";
import type { DesktopPlannerView } from "@contracts";
import type {
  DayTasksResponse,
  OverdueTasksResponse,
  SettingsResponse,
  TaskRecord,
  TaskCreatePayload,
  TaskListResponse,
  WeekTasksResponse,
} from "@/contracts/backend";
import {
  completeTask,
  createTask,
  getCompletedTasks,
  getInboxTasks,
  getOverdueTasks,
  getSettings,
  getTodayTasks,
  getWeekTasks,
  rescheduleTask,
  updateTask,
} from "@/services/backendClient";
import { useDesktopSession } from "@/features/shell/DesktopSessionContext";

export interface PlannerTaskDraft {
  title: string;
  description: string;
  status: string;
  priority: string;
  category: string;
  scheduledDate: string;
  startAt: string;
  endAt: string;
  dueAt: string;
  isAllDay: boolean;
  repeatRule: string;
  estimatedMinutes: string;
  tagsText: string;
}

interface PlannerWorkspaceState {
  today: DayTasksResponse | null;
  week: WeekTasksResponse | null;
  overdue: OverdueTasksResponse | null;
  inbox: TaskListResponse | null;
  completed: TaskListResponse | null;
  settings: SettingsResponse | null;
  loading: boolean;
  mutating: boolean;
  error: string;
  mutationMessage: string;
}

export interface PlannerWorkspace {
  view: DesktopPlannerView;
  setView: (view: DesktopPlannerView) => void;
  state: PlannerWorkspaceState;
  draft: PlannerTaskDraft;
  draftMode: "create" | "edit";
  editingTaskId: string | null;
  setDraft: (updater: (current: PlannerTaskDraft) => PlannerTaskDraft) => void;
  startCreate: (preset?: Partial<PlannerTaskDraft>) => void;
  startEdit: (task: TaskRecord) => void;
  resetDraft: () => void;
  submitDraft: () => Promise<void>;
  refresh: () => Promise<void>;
  markComplete: (task: TaskRecord) => Promise<void>;
  reopenTask: (task: TaskRecord) => Promise<void>;
  markInProgress: (task: TaskRecord) => Promise<void>;
  bumpPriority: (task: TaskRecord) => Promise<void>;
  moveToTomorrow: (task: TaskRecord) => Promise<void>;
  cancelTask: (task: TaskRecord) => Promise<void>;
}

function emptyDraft(preset: Partial<PlannerTaskDraft> = {}): PlannerTaskDraft {
  return {
    title: "",
    description: "",
    status: "planned",
    priority: "medium",
    category: "",
    scheduledDate: "",
    startAt: "",
    endAt: "",
    dueAt: "",
    isAllDay: false,
    repeatRule: "none",
    estimatedMinutes: "",
    tagsText: "",
    ...preset,
  };
}

function isoToLocalDateTimeInput(value: string | null | undefined): string {
  if (!value) {
    return "";
  }

  const trimmed = value.trim();
  return trimmed.length >= 16 ? trimmed.slice(0, 16) : trimmed;
}

function taskToDraft(task: TaskRecord): PlannerTaskDraft {
  return {
    title: task.title,
    description: task.description ?? "",
    status: task.status,
    priority: task.priority,
    category: task.category ?? "",
    scheduledDate: task.scheduled_date ?? "",
    startAt: isoToLocalDateTimeInput(task.start_at),
    endAt: isoToLocalDateTimeInput(task.end_at),
    dueAt: isoToLocalDateTimeInput(task.due_at),
    isAllDay: task.is_all_day,
    repeatRule: task.repeat_rule,
    estimatedMinutes:
      task.estimated_minutes === null || task.estimated_minutes === undefined
        ? ""
        : String(task.estimated_minutes),
    tagsText: task.tags.join(", "),
  };
}

function normalizeOptionalText(value: string): string | null {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}

function normalizeOptionalInt(value: string): number | null {
  const trimmed = value.trim();
  if (!trimmed) {
    return null;
  }

  const parsed = Number.parseInt(trimmed, 10);
  return Number.isFinite(parsed) ? parsed : null;
}

function parseTags(value: string): string[] {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter((item, index, values) => item.length > 0 && values.indexOf(item) === index);
}

function draftToPayload(draft: PlannerTaskDraft): TaskCreatePayload {
  return {
    title: draft.title.trim(),
    description: normalizeOptionalText(draft.description),
    status: draft.status,
    priority: draft.priority,
    category: normalizeOptionalText(draft.category),
    scheduled_date: normalizeOptionalText(draft.scheduledDate),
    start_at: draft.isAllDay ? null : normalizeOptionalText(draft.startAt),
    end_at: draft.isAllDay ? null : normalizeOptionalText(draft.endAt),
    due_at: normalizeOptionalText(draft.dueAt),
    is_all_day: draft.isAllDay,
    repeat_rule: draft.repeatRule,
    estimated_minutes: normalizeOptionalInt(draft.estimatedMinutes),
    actual_minutes: null,
    tags: parseTags(draft.tagsText),
  };
}

function tomorrowDateInput(): string {
  const next = new Date();
  next.setDate(next.getDate() + 1);
  return next.toISOString().slice(0, 10);
}

export function usePlannerWorkspace(): PlannerWorkspace {
  const { sessionState, updateSessionState } = useDesktopSession();
  const [state, setState] = useState<PlannerWorkspaceState>({
    today: null,
    week: null,
    overdue: null,
    inbox: null,
    completed: null,
    settings: null,
    loading: true,
    mutating: false,
    error: "",
    mutationMessage: "",
  });
  const [draft, setDraftState] = useState<PlannerTaskDraft>(() => emptyDraft());
  const [draftMode, setDraftMode] = useState<"create" | "edit">("create");
  const [editingTaskId, setEditingTaskId] = useState<string | null>(null);

  const view = sessionState?.filters.planner_view ?? "today";

  const setView = (nextView: DesktopPlannerView) => {
    updateSessionState((current) => ({
      ...current,
      filters: {
        ...current.filters,
        planner_view: nextView,
      },
    }));
  };

  const refresh = async () => {
    try {
      const [today, week, overdue, inbox, completed, settings] = await Promise.all([
        getTodayTasks(),
        getWeekTasks(),
        getOverdueTasks(),
        getInboxTasks(),
        getCompletedTasks(),
        getSettings(),
      ]);

      startTransition(() => {
        setState((current) => ({
          ...current,
          today,
          week,
          overdue,
          inbox,
          completed,
          settings,
          loading: false,
          error: "",
        }));
      });
    } catch (error) {
      setState((current) => ({
        ...current,
        loading: false,
        error: error instanceof Error ? error.message : String(error),
      }));
    }
  };

  useEffect(() => {
    void refresh();
  }, []);

  const runMutation = async (
    action: () => Promise<TaskRecord>,
    successMessage: string,
    afterSuccess?: (task: TaskRecord) => void,
  ) => {
    setState((current) => ({ ...current, mutating: true, error: "", mutationMessage: "" }));

    try {
      const task = await action();
      afterSuccess?.(task);
      await refresh();
      setState((current) => ({
        ...current,
        mutating: false,
        mutationMessage: successMessage,
      }));
    } catch (error) {
      setState((current) => ({
        ...current,
        mutating: false,
        error: error instanceof Error ? error.message : String(error),
      }));
    }
  };

  const startCreate = (preset: Partial<PlannerTaskDraft> = {}) => {
    const nextPreset: Partial<PlannerTaskDraft> = { ...preset };
    if (!nextPreset.scheduledDate && (view === "today" || view === "reminders")) {
      nextPreset.scheduledDate = state.today?.date ?? "";
    }
    if (!nextPreset.status && view === "inbox") {
      nextPreset.status = "inbox";
    }

    setDraftMode("create");
    setEditingTaskId(null);
    setDraftState(emptyDraft(nextPreset));
    setState((current) => ({ ...current, mutationMessage: "" }));
  };

  const startEdit = (task: TaskRecord) => {
    setDraftMode("edit");
    setEditingTaskId(task.id);
    setDraftState(taskToDraft(task));
    setState((current) => ({ ...current, mutationMessage: "" }));
  };

  const resetDraft = () => {
    if (draftMode === "edit" && editingTaskId) {
      const task = [
        ...(state.today?.items ?? []),
        ...(state.overdue?.items ?? []),
        ...(state.inbox?.items ?? []),
        ...(state.completed?.items ?? []),
        ...((state.week?.days ?? []).flatMap((day) => day.items)),
      ].find((item) => item.id === editingTaskId);

      if (task) {
        setDraftState(taskToDraft(task));
        return;
      }
    }

    startCreate();
  };

  const setDraft = (updater: (current: PlannerTaskDraft) => PlannerTaskDraft) => {
    setDraftState((current) => updater(current));
  };

  const submitDraft = async () => {
    if (!draft.title.trim()) {
      setState((current) => ({ ...current, error: "Task title must not be empty." }));
      return;
    }

    const payload = draftToPayload(draft);
    if (draftMode === "edit" && editingTaskId) {
      await runMutation(
        () => updateTask(editingTaskId, payload),
        "Task updated.",
      );
      return;
    }

    await runMutation(
      () => createTask(payload),
      "Task created.",
      () => {
        setDraftMode("create");
        setEditingTaskId(null);
        setDraftState(
          emptyDraft({
            scheduledDate: draft.scheduledDate,
            tagsText: draft.tagsText,
          }),
        );
      },
    );
  };

  const workspace = useMemo<PlannerWorkspace>(
    () => ({
      view,
      setView,
      state,
      draft,
      draftMode,
      editingTaskId,
      setDraft,
      startCreate,
      startEdit,
      resetDraft,
      submitDraft,
      refresh,
      async markComplete(task: TaskRecord) {
        await runMutation(
          () => completeTask(task.id),
          `Marked '${task.title}' complete.`,
        );
      },
      async reopenTask(task: TaskRecord) {
        const nextStatus = task.scheduled_date ? "planned" : "inbox";
        await runMutation(
          () => updateTask(task.id, { status: nextStatus }),
          `Reopened '${task.title}'.`,
        );
      },
      async markInProgress(task: TaskRecord) {
        await runMutation(
          () => updateTask(task.id, { status: "in_progress" }),
          `Started '${task.title}'.`,
        );
      },
      async bumpPriority(task: TaskRecord) {
        const nextPriority =
          task.priority === "low"
            ? "medium"
            : task.priority === "medium"
              ? "high"
              : "critical";
        await runMutation(
          () => updateTask(task.id, { priority: nextPriority }),
          `Updated '${task.title}' priority to ${nextPriority}.`,
        );
      },
      async moveToTomorrow(task: TaskRecord) {
        await runMutation(
          () =>
            rescheduleTask(task.id, {
              scheduled_date: tomorrowDateInput(),
              start_at: null,
              end_at: null,
              due_at: null,
            }),
          `Moved '${task.title}' to tomorrow.`,
        );
      },
      async cancelTask(task: TaskRecord) {
        await runMutation(
          () => updateTask(task.id, { status: "cancelled" }),
          `Cancelled '${task.title}'.`,
        );
      },
    }),
    [draft, draftMode, editingTaskId, state, view],
  );

  return workspace;
}
