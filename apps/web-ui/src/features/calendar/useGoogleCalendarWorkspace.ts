import { startTransition, useEffect, useState } from "react";
import type {
  GoogleCalendarEventCreatePayload,
  GoogleCalendarEventListResponse,
  GoogleCalendarEventRecord,
  GoogleCalendarEventUpdatePayload,
  GoogleCalendarStatusResponse,
} from "@/contracts/backend";
import {
  createGoogleCalendarEvent,
  deleteGoogleCalendarEvent,
  getGoogleCalendarStatus,
  listGoogleCalendarEvents,
  updateGoogleCalendarEvent,
} from "@/services/backendClient";

export interface CalendarEventDraft {
  summary: string;
  description: string;
  location: string;
  attendeesText: string;
  calendarId: string;
  isAllDay: boolean;
  startAt: string;
  endAt: string;
  startDate: string;
  endDate: string;
}

interface GoogleCalendarWorkspaceState {
  status: GoogleCalendarStatusResponse | null;
  agenda: GoogleCalendarEventListResponse | null;
  windowStartDate: string;
  loading: boolean;
  mutating: boolean;
  error: string;
  message: string;
}

export interface GoogleCalendarWorkspace {
  state: GoogleCalendarWorkspaceState;
  draft: CalendarEventDraft;
  draftMode: "create" | "edit";
  editingEventId: string | null;
  setDraft: (updater: (current: CalendarEventDraft) => CalendarEventDraft) => void;
  setWindowStartDate: (value: string) => void;
  startCreate: (preset?: Partial<CalendarEventDraft>) => void;
  startEdit: (event: GoogleCalendarEventRecord) => void;
  resetDraft: () => void;
  submitDraft: () => Promise<void>;
  deleteEvent: (event: GoogleCalendarEventRecord) => Promise<void>;
  refresh: () => Promise<void>;
}

function todayInput(): string {
  const value = new Date();
  const timezoneOffset = value.getTimezoneOffset() * 60_000;
  return new Date(value.getTime() - timezoneOffset).toISOString().slice(0, 10);
}

function defaultTimedInput(offsetHours: number): string {
  const value = new Date();
  value.setMinutes(0, 0, 0);
  value.setHours(value.getHours() + offsetHours);
  const timezoneOffset = value.getTimezoneOffset() * 60_000;
  return new Date(value.getTime() - timezoneOffset).toISOString().slice(0, 16);
}

function emptyDraft(preset: Partial<CalendarEventDraft> = {}): CalendarEventDraft {
  const today = todayInput();
  return {
    summary: "",
    description: "",
    location: "",
    attendeesText: "",
    calendarId: "",
    isAllDay: false,
    startAt: defaultTimedInput(1),
    endAt: defaultTimedInput(2),
    startDate: today,
    endDate: today,
    ...preset,
  };
}

function isoToLocalDateTimeInput(value: string | null | undefined): string {
  if (!value) {
    return "";
  }
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value.trim().slice(0, 16);
  }
  const timezoneOffset = parsed.getTimezoneOffset() * 60_000;
  return new Date(parsed.getTime() - timezoneOffset).toISOString().slice(0, 16);
}

function eventToDraft(event: GoogleCalendarEventRecord): CalendarEventDraft {
  return {
    summary: event.summary,
    description: event.description ?? "",
    location: event.location ?? "",
    attendeesText: event.attendees.join(", "),
    calendarId: event.calendar_id === "primary" ? "" : event.calendar_id,
    isAllDay: event.is_all_day,
    startAt: isoToLocalDateTimeInput(event.start_at),
    endAt: isoToLocalDateTimeInput(event.end_at),
    startDate: event.start_date ?? todayInput(),
    endDate: event.end_date ?? event.start_date ?? todayInput(),
  };
}

function parseAttendees(value: string): string[] {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter((item, index, values) => item.length > 0 && values.indexOf(item) === index);
}

function draftToPayload(
  draft: CalendarEventDraft,
): GoogleCalendarEventCreatePayload | GoogleCalendarEventUpdatePayload {
  const calendarId = draft.calendarId.trim();
  return {
    summary: draft.summary.trim(),
    description: draft.description.trim() || null,
    location: draft.location.trim() || null,
    attendees: parseAttendees(draft.attendeesText),
    calendar_id: calendarId || null,
    is_all_day: draft.isAllDay,
    start_at: draft.isAllDay ? null : draft.startAt || null,
    end_at: draft.isAllDay ? null : draft.endAt || null,
    start_date: draft.isAllDay ? draft.startDate || null : null,
    end_date: draft.isAllDay ? draft.endDate || draft.startDate || null : null,
  };
}

export function useGoogleCalendarWorkspace(): GoogleCalendarWorkspace {
  const [state, setState] = useState<GoogleCalendarWorkspaceState>({
    status: null,
    agenda: null,
    windowStartDate: todayInput(),
    loading: true,
    mutating: false,
    error: "",
    message: "",
  });
  const [draft, setDraftState] = useState<CalendarEventDraft>(() => emptyDraft());
  const [draftMode, setDraftMode] = useState<"create" | "edit">("create");
  const [editingEventId, setEditingEventId] = useState<string | null>(null);

  const refresh = async () => {
    setState((current) => ({ ...current, loading: true, error: "" }));
    try {
      const status = await getGoogleCalendarStatus();
      const agenda = await listGoogleCalendarEvents({
        start_date: state.windowStartDate,
        days: status.agenda_days,
        calendar_id: status.default_calendar_id,
        limit: status.event_limit,
      });
      startTransition(() => {
        setState((current) => ({
          ...current,
          status,
          agenda,
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
  }, [state.windowStartDate]);

  const setDraft = (updater: (current: CalendarEventDraft) => CalendarEventDraft) => {
    setDraftState((current) => updater(current));
  };

  const setWindowStartDate = (value: string) => {
    setState((current) => ({ ...current, windowStartDate: value || todayInput() }));
  };

  const startCreate = (preset: Partial<CalendarEventDraft> = {}) => {
    setDraftMode("create");
    setEditingEventId(null);
    setDraftState(
      emptyDraft({
        calendarId: state.status?.default_calendar_id === "primary" ? "" : state.status?.default_calendar_id ?? "",
        ...preset,
      }),
    );
    setState((current) => ({ ...current, message: "" }));
  };

  const startEdit = (event: GoogleCalendarEventRecord) => {
    setDraftMode("edit");
    setEditingEventId(event.id);
    setDraftState(eventToDraft(event));
    setState((current) => ({ ...current, message: "" }));
  };

  const resetDraft = () => {
    if (draftMode === "edit" && editingEventId) {
      const event = state.agenda?.items.find((item) => item.id === editingEventId);
      if (event) {
        setDraftState(eventToDraft(event));
        return;
      }
    }
    startCreate();
  };

  const submitDraft = async () => {
    if (!draft.summary.trim()) {
      setState((current) => ({ ...current, error: "Calendar summary must not be empty." }));
      return;
    }
    setState((current) => ({ ...current, mutating: true, error: "", message: "" }));
    try {
      if (draftMode === "edit" && editingEventId) {
        await updateGoogleCalendarEvent(
          editingEventId,
          draftToPayload(draft) as GoogleCalendarEventUpdatePayload,
        );
        await refresh();
        setState((current) => ({
          ...current,
          mutating: false,
          message: "Calendar event updated.",
        }));
        return;
      }
      await createGoogleCalendarEvent(draftToPayload(draft) as GoogleCalendarEventCreatePayload);
      setDraftMode("create");
      setEditingEventId(null);
      setDraftState(emptyDraft());
      await refresh();
      setState((current) => ({
        ...current,
        mutating: false,
        message: "Calendar event created.",
      }));
    } catch (error) {
      setState((current) => ({
        ...current,
        mutating: false,
        error: error instanceof Error ? error.message : String(error),
      }));
    }
  };

  const deleteEvent = async (event: GoogleCalendarEventRecord) => {
    setState((current) => ({ ...current, mutating: true, error: "", message: "" }));
    try {
      await deleteGoogleCalendarEvent(event.id, event.calendar_id);
      if (editingEventId === event.id) {
        setDraftMode("create");
        setEditingEventId(null);
        setDraftState(emptyDraft());
      }
      await refresh();
      setState((current) => ({
        ...current,
        mutating: false,
        message: "Calendar event deleted.",
      }));
    } catch (error) {
      setState((current) => ({
        ...current,
        mutating: false,
        error: error instanceof Error ? error.message : String(error),
      }));
    }
  };

  return {
    state,
    draft,
    draftMode,
    editingEventId,
    setDraft,
    setWindowStartDate,
    startCreate,
    startEdit,
    resetDraft,
    submitDraft,
    deleteEvent,
    refresh,
  };
}
