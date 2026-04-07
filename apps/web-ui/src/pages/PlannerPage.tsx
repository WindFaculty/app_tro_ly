import { useDeferredValue, useMemo, useState } from "react";
import type { DesktopPlannerView } from "@contracts";
import { PageTemplate } from "@/components/PageTemplate";
import type { SettingsResponse, TaskRecord, WeekDaySummary } from "@/contracts/backend";
import { usePlannerWorkspace } from "@/features/planner/usePlannerWorkspace";
import styles from "./PlannerPage.module.css";

const VIEW_LABELS: Array<{ view: DesktopPlannerView; label: string; hint: string }> = [
  { view: "today", label: "Today", hint: "Daily agenda" },
  { view: "week", label: "Week", hint: "Seven-day plan" },
  { view: "calendar", label: "Calendar", hint: "Week grid" },
  { view: "inbox", label: "Inbox", hint: "Unscheduled" },
  { view: "overdue", label: "Overdue", hint: "Needs rescue" },
  { view: "completed", label: "Completed", hint: "Closed loop" },
  { view: "reminders", label: "Reminders", hint: "Due-soon lane" },
  { view: "tags", label: "Tags", hint: "Organize work" },
];

interface ReminderLaneItem {
  task: TaskRecord;
  kind: "due_soon" | "overdue" | "upcoming";
  anchor: string;
}

interface TagBucket {
  tag: string;
  count: number;
}

function statusTone(status: string): "accent" | "sun" | "success" | "danger" | "warm" {
  switch (status) {
    case "done":
      return "success";
    case "cancelled":
      return "danger";
    case "in_progress":
      return "warm";
    case "inbox":
      return "sun";
    default:
      return "accent";
  }
}

function priorityTone(priority: string): "accent" | "sun" | "success" | "danger" | "warm" {
  switch (priority) {
    case "critical":
      return "danger";
    case "high":
      return "warm";
    case "medium":
      return "sun";
    default:
      return "accent";
  }
}

function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return "No time";
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

function formatDate(value: string | null | undefined): string {
  if (!value) {
    return "Unscheduled";
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
  }).format(parsed);
}

function taskAnchor(task: TaskRecord): string {
  return task.start_at ?? task.due_at ?? task.scheduled_date ?? task.created_at;
}

function searchHaystack(task: TaskRecord): string {
  return [
    task.title,
    task.description ?? "",
    task.category ?? "",
    task.priority,
    task.status,
    task.scheduled_date ?? "",
    task.tags.join(" "),
  ]
    .join(" ")
    .toLowerCase();
}

function matchesTask(task: TaskRecord, query: string, activeTag: string | null): boolean {
  const searchMatch = !query || searchHaystack(task).includes(query);
  const tagMatch = !activeTag || task.tags.includes(activeTag);
  return searchMatch && tagMatch;
}

function dedupeTasks(tasks: TaskRecord[]): TaskRecord[] {
  const byId = new Map<string, TaskRecord>();
  for (const task of tasks) {
    if (!byId.has(task.id)) {
      byId.set(task.id, task);
    }
  }
  return Array.from(byId.values());
}

function reminderLeadMinutes(settings: SettingsResponse | null): number {
  const value = (settings?.reminder as { lead_minutes?: unknown } | undefined)?.lead_minutes;
  return typeof value === "number" ? value : 15;
}

function deriveReminderItems(
  todayTasks: TaskRecord[],
  dueSoonTasks: TaskRecord[],
  overdueTasks: TaskRecord[],
  weekDays: WeekDaySummary[],
): ReminderLaneItem[] {
  const items: ReminderLaneItem[] = [];
  const seen = new Set<string>();

  for (const task of dueSoonTasks) {
    items.push({ task, kind: "due_soon", anchor: taskAnchor(task) });
    seen.add(task.id);
  }

  for (const task of overdueTasks) {
    if (!seen.has(task.id)) {
      items.push({ task, kind: "overdue", anchor: taskAnchor(task) });
      seen.add(task.id);
    }
  }

  const upcoming = weekDays
    .flatMap((day) => day.items)
    .filter((task) => !seen.has(task.id) && !todayTasks.some((todayTask) => todayTask.id === task.id))
    .sort((left, right) => taskAnchor(left).localeCompare(taskAnchor(right)))
    .slice(0, 8);

  for (const task of upcoming) {
    items.push({ task, kind: "upcoming", anchor: taskAnchor(task) });
  }

  return items;
}

function kindLabel(kind: ReminderLaneItem["kind"]): string {
  switch (kind) {
    case "due_soon":
      return "Due soon";
    case "overdue":
      return "Overdue";
    default:
      return "Upcoming";
  }
}

function buildTagBuckets(tasks: TaskRecord[]): TagBucket[] {
  const counts = new Map<string, number>();
  for (const task of tasks) {
    for (const tag of task.tags) {
      counts.set(tag, (counts.get(tag) ?? 0) + 1);
    }
  }

  return Array.from(counts.entries())
    .map(([tag, count]) => ({ tag, count }))
    .sort((left, right) => right.count - left.count || left.tag.localeCompare(right.tag));
}

function TaskCard({
  task,
  onEdit,
  onComplete,
  onReopen,
  onStart,
  onBumpPriority,
  onMoveTomorrow,
  onCancel,
}: {
  task: TaskRecord;
  onEdit: (task: TaskRecord) => void;
  onComplete: (task: TaskRecord) => void;
  onReopen: (task: TaskRecord) => void;
  onStart: (task: TaskRecord) => void;
  onBumpPriority: (task: TaskRecord) => void;
  onMoveTomorrow: (task: TaskRecord) => void;
  onCancel: (task: TaskRecord) => void;
}) {
  return (
    <article className={styles.taskCard}>
      <div className={styles.taskHeader}>
        <div className={styles.taskTitleBlock}>
          <p className="listTitle">{task.title}</p>
          <p className="listSubtitle">
            {task.category || "General"} | {formatDate(task.scheduled_date)} |{" "}
            {formatDateTime(task.start_at || task.due_at)}
          </p>
        </div>
        <div className="chipRow">
          <span className="chip" data-tone={statusTone(task.status)}>
            {task.status}
          </span>
          <span className="chip" data-tone={priorityTone(task.priority)}>
            {task.priority}
          </span>
        </div>
      </div>

      {task.description && <p className="bodyText">{task.description}</p>}

      <div className="chipRow">
        {task.tags.length > 0 ? (
          task.tags.map((tag) => (
            <span key={tag} className="chip" data-tone="accent">
              #{tag}
            </span>
          ))
        ) : (
          <span className="chip" data-tone="sun">
            no tags
          </span>
        )}
        {task.repeat_rule !== "none" && (
          <span className="chip" data-tone="warm">
            repeats {task.repeat_rule}
          </span>
        )}
      </div>

      <div className={styles.taskActions}>
        <button type="button" className="ghostButton" onClick={() => onEdit(task)}>
          Edit
        </button>
        {task.status === "done" ? (
          <button type="button" className="ghostButton" onClick={() => onReopen(task)}>
            Reopen
          </button>
        ) : (
          <button type="button" className="secondaryButton" onClick={() => onComplete(task)}>
            Complete
          </button>
        )}
        {task.status !== "in_progress" && task.status !== "done" && task.status !== "cancelled" && (
          <button type="button" className="ghostButton" onClick={() => onStart(task)}>
            Start
          </button>
        )}
        <button type="button" className="ghostButton" onClick={() => onBumpPriority(task)}>
          Raise priority
        </button>
        <button type="button" className="ghostButton" onClick={() => onMoveTomorrow(task)}>
          Tomorrow
        </button>
        {task.status !== "cancelled" && task.status !== "done" && (
          <button type="button" className="ghostButton" onClick={() => onCancel(task)}>
            Cancel
          </button>
        )}
      </div>
    </article>
  );
}

export function PlannerPage() {
  const workspace = usePlannerWorkspace();
  const [searchQuery, setSearchQuery] = useState("");
  const [activeTag, setActiveTag] = useState<string | null>(null);
  const deferredSearch = useDeferredValue(searchQuery.trim().toLowerCase());

  const todayItems = workspace.state.today?.items ?? [];
  const weekDays = workspace.state.week?.days ?? [];
  const overdueItems = workspace.state.overdue?.items ?? [];
  const inboxItems = workspace.state.inbox?.items ?? [];
  const completedItems = workspace.state.completed?.items ?? [];
  const reminderItems = useMemo(
    () =>
      deriveReminderItems(
        todayItems,
        workspace.state.today?.due_soon ?? [],
        overdueItems,
        weekDays,
      ),
    [overdueItems, todayItems, weekDays, workspace.state.today?.due_soon],
  );

  const allTasks = useMemo(
    () =>
      dedupeTasks([
        ...todayItems,
        ...overdueItems,
        ...inboxItems,
        ...completedItems,
        ...weekDays.flatMap((day) => day.items),
      ]),
    [completedItems, inboxItems, overdueItems, todayItems, weekDays],
  );

  const tagBuckets = useMemo(() => buildTagBuckets(allTasks), [allTasks]);
  const filteredToday = todayItems.filter((task) => matchesTask(task, deferredSearch, activeTag));
  const filteredOverdue = overdueItems.filter((task) => matchesTask(task, deferredSearch, activeTag));
  const filteredInbox = inboxItems.filter((task) => matchesTask(task, deferredSearch, activeTag));
  const filteredCompleted = completedItems.filter((task) =>
    matchesTask(task, deferredSearch, activeTag),
  );
  const filteredWeekDays = weekDays.map((day) => ({
    ...day,
    items: day.items.filter((task) => matchesTask(task, deferredSearch, activeTag)),
  }));
  const filteredReminderItems = reminderItems.filter(({ task }) =>
    matchesTask(task, deferredSearch, activeTag),
  );
  const filteredTagTasks = allTasks.filter((task) => matchesTask(task, deferredSearch, activeTag));

  const leadMinutes = reminderLeadMinutes(workspace.state.settings);
  const activeCount = allTasks.filter((task) => !["done", "cancelled"].includes(task.status)).length;

  const handleStartCreate = () => {
    workspace.startCreate(activeTag ? { tagsText: activeTag } : undefined);
  };

  const renderTaskList = (tasks: TaskRecord[], emptyTitle: string, emptyText: string) =>
    tasks.length > 0 ? (
      <div className={styles.taskList}>
        {tasks.map((task) => (
          <TaskCard
            key={task.id}
            task={task}
            onEdit={workspace.startEdit}
            onComplete={(item) => void workspace.markComplete(item)}
            onReopen={(item) => void workspace.reopenTask(item)}
            onStart={(item) => void workspace.markInProgress(item)}
            onBumpPriority={(item) => void workspace.bumpPriority(item)}
            onMoveTomorrow={(item) => void workspace.moveToTomorrow(item)}
            onCancel={(item) => void workspace.cancelTask(item)}
          />
        ))}
      </div>
    ) : (
      <div className="emptyState">
        <p className="emptyStateTitle">{emptyTitle}</p>
        <p className="emptyStateText">{emptyText}</p>
      </div>
    );

  const renderViewSurface = () => {
    switch (workspace.view) {
      case "today":
        return (
          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Today board</span>
                <h3 className="surfaceTitle">Current agenda and due-soon lane</h3>
                <p className="surfaceIntro">
                  Tasks scheduled for {workspace.state.today?.date ?? "today"} with reminder-ready
                  signals pulled from the backend day snapshot.
                </p>
              </div>
              <span className="chip" data-tone="accent">
                {workspace.state.today?.in_progress.length ?? 0} in progress
              </span>
            </div>

            <div className={styles.surfaceStack}>
              {workspace.state.today?.due_soon.length ? (
                <div className={styles.inlineAlert}>
                  <span className="chip" data-tone="warm">
                    due soon
                  </span>
                  <p className="bodyText">
                    {workspace.state.today.due_soon.length} task(s) are inside the next reminder
                    window.
                  </p>
                </div>
              ) : null}
              {renderTaskList(
                filteredToday,
                "No tasks for today",
                "Use the task editor below to add a scheduled item or change filters.",
              )}
            </div>
          </article>
        );
      case "week":
        return (
          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Week agenda</span>
                <h3 className="surfaceTitle">Seven-day plan with conflicts</h3>
                <p className="surfaceIntro">
                  Agenda mode keeps the week readable in a narrow desktop rail while still surfacing
                  conflicts and high-priority clusters.
                </p>
              </div>
              <span className="chip" data-tone={workspace.state.week?.conflicts.length ? "warm" : "accent"}>
                {workspace.state.week?.conflicts.length ?? 0} conflicts
              </span>
            </div>
            <div className={styles.dayList}>
              {filteredWeekDays.map((day) => (
                <article key={day.date} className={styles.dayCard}>
                  <div className={styles.dayHeader}>
                    <div>
                      <p className="listTitle">{formatDate(day.date)}</p>
                      <p className="listSubtitle">
                        {day.task_count} tasks | {day.high_priority_count} high priority
                      </p>
                    </div>
                    <span className="chip" data-tone={day.high_priority_count > 0 ? "warm" : "accent"}>
                      {day.items.length} shown
                    </span>
                  </div>
                  {renderTaskList(
                    day.items,
                    "No visible tasks",
                    "This day has no tasks matching the current search or tag filter.",
                  )}
                </article>
              ))}
            </div>
          </article>
        );
      case "calendar":
        return (
          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Calendar view</span>
                <h3 className="surfaceTitle">Week grid for date-first planning</h3>
                <p className="surfaceIntro">
                  This is the landed planner calendar view for local tasks. Google calendar data
                  still belongs to A10.
                </p>
              </div>
            </div>
            <div className={styles.calendarGrid}>
              {filteredWeekDays.map((day) => (
                <article key={day.date} className={styles.calendarDay}>
                  <div className={styles.calendarDayHeader}>
                    <p className="listTitle">{formatDate(day.date)}</p>
                    <span className="chip" data-tone="accent">
                      {day.items.length}
                    </span>
                  </div>
                  <div className={styles.calendarTaskList}>
                    {day.items.length > 0 ? (
                      day.items.map((task) => (
                        <button
                          key={task.id}
                          type="button"
                          className={styles.calendarTask}
                          onClick={() => workspace.startEdit(task)}
                        >
                          <span className={styles.calendarTaskTitle}>{task.title}</span>
                          <span className={styles.calendarTaskMeta}>
                            {formatDateTime(task.start_at || task.due_at)}
                          </span>
                        </button>
                      ))
                    ) : (
                      <p className="helperText">No tasks</p>
                    )}
                  </div>
                </article>
              ))}
            </div>
          </article>
        );
      case "inbox":
        return (
          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Inbox</span>
                <h3 className="surfaceTitle">Unscheduled task capture</h3>
                <p className="surfaceIntro">
                  Items without a confirmed date stay here until they are promoted into the schedule.
                </p>
              </div>
              <span className="chip" data-tone="sun">
                {workspace.state.inbox?.count ?? 0} items
              </span>
            </div>
            {renderTaskList(
              filteredInbox,
              "Inbox is clear",
              "Quick-capture tasks will appear here until they gain a planned date.",
            )}
          </article>
        );
      case "overdue":
        return (
          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Overdue rescue</span>
                <h3 className="surfaceTitle">Items that already missed their anchor</h3>
                <p className="surfaceIntro">
                  Overdue tasks are sourced directly from the backend overdue query, not guessed from
                  client state.
                </p>
              </div>
            </div>
            {renderTaskList(
              filteredOverdue,
              "No overdue tasks",
              "Nothing is currently past due in the backend task store.",
            )}
          </article>
        );
      case "completed":
        return (
          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Completed log</span>
                <h3 className="surfaceTitle">Recently closed work</h3>
                <p className="surfaceIntro">
                  Completed tasks stay searchable and reopenable from the planner module.
                </p>
              </div>
            </div>
            {renderTaskList(
              filteredCompleted,
              "No completed tasks yet",
              "Completed work will show here once tasks start closing out.",
            )}
          </article>
        );
      case "reminders":
        return (
          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Reminders</span>
                <h3 className="surfaceTitle">Due-soon and upcoming reminder lane</h3>
                <p className="surfaceIntro">
                  Reminder timing uses the current backend setting of {leadMinutes} minutes before
                  the task anchor.
                </p>
              </div>
              <span className="chip" data-tone={filteredReminderItems.length ? "warm" : "accent"}>
                {filteredReminderItems.length} reminder signals
              </span>
            </div>
            {filteredReminderItems.length > 0 ? (
              <div className={styles.reminderList}>
                {filteredReminderItems.map((item) => (
                  <article key={`${item.kind}-${item.task.id}`} className={styles.reminderCard}>
                    <div className={styles.reminderHeader}>
                      <div>
                        <p className="listTitle">{item.task.title}</p>
                        <p className="listSubtitle">
                          {kindLabel(item.kind)} | anchor {formatDateTime(item.anchor)}
                        </p>
                      </div>
                      <span className="chip" data-tone={item.kind === "overdue" ? "danger" : item.kind === "due_soon" ? "warm" : "accent"}>
                        {kindLabel(item.kind)}
                      </span>
                    </div>
                    <div className={styles.taskActions}>
                      <button type="button" className="ghostButton" onClick={() => workspace.startEdit(item.task)}>
                        Edit
                      </button>
                      <button type="button" className="secondaryButton" onClick={() => void workspace.markComplete(item.task)}>
                        Complete
                      </button>
                      <button type="button" className="ghostButton" onClick={() => void workspace.moveToTomorrow(item.task)}>
                        Tomorrow
                      </button>
                    </div>
                  </article>
                ))}
              </div>
            ) : (
              <div className="emptyState">
                <p className="emptyStateTitle">No reminder signals right now</p>
                <p className="emptyStateText">
                  Due-soon and upcoming anchors will appear here as tasks enter the reminder window.
                </p>
              </div>
            )}
          </article>
        );
      case "tags":
        return (
          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Tags</span>
                <h3 className="surfaceTitle">Filter and reuse task organization tags</h3>
                <p className="surfaceIntro">
                  Tags already live in the backend task schema, so this view works without inventing
                  a separate tag store.
                </p>
              </div>
              <span className="chip" data-tone="accent">
                {tagBuckets.length} tags
              </span>
            </div>
            <div className={styles.tagRail}>
              {tagBuckets.map((bucket) => (
                <button
                  key={bucket.tag}
                  type="button"
                  className={`${styles.tagButton} ${activeTag === bucket.tag ? styles.tagButtonActive : ""}`}
                  onClick={() => setActiveTag((current) => (current === bucket.tag ? null : bucket.tag))}
                >
                  <span>#{bucket.tag}</span>
                  <span className={styles.tagCount}>{bucket.count}</span>
                </button>
              ))}
            </div>
            {renderTaskList(
              filteredTagTasks,
              "No tasks match the selected tag",
              "Choose another tag or clear the current tag filter.",
            )}
          </article>
        );
      default:
        return null;
    }
  };

  return (
    <PageTemplate
      title="Planner"
      icon="PL"
      eyebrow="Planning lane"
      description="A07 turns the planner lane into a real task workspace with CRUD, week and calendar views, reminder handling, tag filtering, and persisted planner-view restore."
      actions={
        <>
          <button
            type="button"
            className="secondaryButton"
            onClick={handleStartCreate}
            disabled={workspace.state.mutating}
          >
            New task
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
      highlights={[
        {
          label: "Active",
          value: String(activeCount),
          detail: "Open tasks across planner views",
        },
        {
          label: "Due soon",
          value: String(workspace.state.today?.due_soon.length ?? 0),
          detail: `${leadMinutes} minute reminder lead`,
        },
        {
          label: "Overdue",
          value: String(overdueItems.length),
          detail: "Direct backend overdue query",
        },
        {
          label: "Tags",
          value: String(tagBuckets.length),
          detail: activeTag ? `Filtering #${activeTag}` : "No tag filter",
        },
      ]}
    >
      <div className="appStack">
        <article className="surface surfaceHero">
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Workspace mode</span>
              <h3 className="surfaceTitle">Planner, reminders, tags, and calendar views</h3>
              <p className="surfaceIntro">
                The selected planner view now persists through the desktop restore layer instead of
                resetting to a placeholder page.
              </p>
            </div>
            <span className="chip" data-tone="accent">
              restore {workspace.view}
            </span>
          </div>

          <div className={styles.viewSwitcher}>
            {VIEW_LABELS.map((item) => (
              <button
                key={item.view}
                type="button"
                className={`${styles.viewButton} ${workspace.view === item.view ? styles.viewButtonActive : ""}`}
                onClick={() => workspace.setView(item.view)}
              >
                <span>{item.label}</span>
                <span className={styles.viewHint}>{item.hint}</span>
              </button>
            ))}
          </div>

          <div className={styles.filterRow}>
            <input
              type="search"
              className="textInput"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Search tasks by title, description, category, priority, or tag"
            />
            <div className="chipRow">
              <span className="chip" data-tone={activeTag ? "warm" : "accent"}>
                {activeTag ? `tag #${activeTag}` : "all tags"}
              </span>
              <span className="chip" data-tone={deferredSearch ? "sun" : "accent"}>
                {deferredSearch ? "search active" : "search ready"}
              </span>
            </div>
          </div>

          {tagBuckets.length > 0 && (
            <div className="chipRow">
              {tagBuckets.slice(0, 8).map((bucket) => (
                <button
                  key={bucket.tag}
                  type="button"
                  className="ghostButton"
                  onClick={() => setActiveTag((current) => (current === bucket.tag ? null : bucket.tag))}
                >
                  #{bucket.tag} ({bucket.count})
                </button>
              ))}
              {activeTag && (
                <button type="button" className="ghostButton" onClick={() => setActiveTag(null)}>
                  Clear tag
                </button>
              )}
            </div>
          )}
        </article>

        {renderViewSurface()}

        <div className="surfaceGrid">
          <article className="surface surfaceMuted">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Task editor</span>
                <h3 className="surfaceTitle">
                  {workspace.draftMode === "edit" ? "Edit selected task" : "Create a new task"}
                </h3>
                <p className="surfaceIntro">
                  This editor writes directly to the existing backend task contract and keeps tags
                  inside the same task payload.
                </p>
              </div>
              <span className="chip" data-tone={workspace.draftMode === "edit" ? "warm" : "accent"}>
                {workspace.draftMode}
              </span>
            </div>

            <div className={styles.editorGrid}>
              <label className={styles.editorField}>
                <span className="formLabel">Title</span>
                <input
                  className="textInput"
                  value={workspace.draft.title}
                  onChange={(event) =>
                    workspace.setDraft((current) => ({ ...current, title: event.target.value }))
                  }
                  placeholder="Review release checklist"
                />
              </label>

              <label className={styles.editorField}>
                <span className="formLabel">Description</span>
                <textarea
                  className="textArea"
                  rows={4}
                  value={workspace.draft.description}
                  onChange={(event) =>
                    workspace.setDraft((current) => ({ ...current, description: event.target.value }))
                  }
                  placeholder="Optional task context"
                />
              </label>

              <div className={styles.editorSplit}>
                <label className={styles.editorField}>
                  <span className="formLabel">Status</span>
                  <select
                    className="textInput"
                    value={workspace.draft.status}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({ ...current, status: event.target.value }))
                    }
                  >
                    <option value="inbox">Inbox</option>
                    <option value="planned">Planned</option>
                    <option value="in_progress">In progress</option>
                    <option value="done">Done</option>
                    <option value="cancelled">Cancelled</option>
                  </select>
                </label>

                <label className={styles.editorField}>
                  <span className="formLabel">Priority</span>
                  <select
                    className="textInput"
                    value={workspace.draft.priority}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({ ...current, priority: event.target.value }))
                    }
                  >
                    <option value="low">Low</option>
                    <option value="medium">Medium</option>
                    <option value="high">High</option>
                    <option value="critical">Critical</option>
                  </select>
                </label>
              </div>

              <div className={styles.editorSplit}>
                <label className={styles.editorField}>
                  <span className="formLabel">Scheduled date</span>
                  <input
                    type="date"
                    className="textInput"
                    value={workspace.draft.scheduledDate}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({ ...current, scheduledDate: event.target.value }))
                    }
                  />
                </label>

                <label className={styles.editorField}>
                  <span className="formLabel">Estimated minutes</span>
                  <input
                    type="number"
                    min="0"
                    className="textInput"
                    value={workspace.draft.estimatedMinutes}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({ ...current, estimatedMinutes: event.target.value }))
                    }
                  />
                </label>
              </div>

              <div className={styles.editorSplit}>
                <label className={styles.editorField}>
                  <span className="formLabel">Start at</span>
                  <input
                    type="datetime-local"
                    className="textInput"
                    value={workspace.draft.startAt}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({ ...current, startAt: event.target.value }))
                    }
                    disabled={workspace.draft.isAllDay}
                  />
                </label>

                <label className={styles.editorField}>
                  <span className="formLabel">Due at</span>
                  <input
                    type="datetime-local"
                    className="textInput"
                    value={workspace.draft.dueAt}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({ ...current, dueAt: event.target.value }))
                    }
                  />
                </label>
              </div>

              <div className={styles.editorSplit}>
                <label className={styles.editorField}>
                  <span className="formLabel">Category</span>
                  <input
                    className="textInput"
                    value={workspace.draft.category}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({ ...current, category: event.target.value }))
                    }
                    placeholder="Work, personal, admin"
                  />
                </label>

                <label className={styles.editorField}>
                  <span className="formLabel">Repeat</span>
                  <select
                    className="textInput"
                    value={workspace.draft.repeatRule}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({ ...current, repeatRule: event.target.value }))
                    }
                  >
                    <option value="none">None</option>
                    <option value="daily">Daily</option>
                    <option value="weekdays">Weekdays</option>
                    <option value="weekly">Weekly</option>
                    <option value="monthly">Monthly</option>
                  </select>
                </label>
              </div>

              <label className={styles.editorField}>
                <span className="formLabel">Tags</span>
                <input
                  className="textInput"
                  value={workspace.draft.tagsText}
                  onChange={(event) =>
                    workspace.setDraft((current) => ({ ...current, tagsText: event.target.value }))
                  }
                  placeholder="focus, release, personal"
                />
              </label>

              <label className={styles.checkboxRow}>
                <input
                  type="checkbox"
                  checked={workspace.draft.isAllDay}
                  onChange={(event) =>
                    workspace.setDraft((current) => ({ ...current, isAllDay: event.target.checked }))
                  }
                />
                <span className="helperText">All-day task</span>
              </label>
            </div>

            <div className="actionRow">
              <span className="helperText">
                {workspace.state.mutationMessage || "Task writes go through the current FastAPI task contract."}
              </span>
              <div className="chipRow">
                <button type="button" className="ghostButton" onClick={workspace.resetDraft}>
                  Reset
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
                      ? "Save changes"
                      : "Create task"}
                </button>
              </div>
            </div>
          </article>

          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Week conflicts</span>
                <h3 className="surfaceTitle">Scheduling collisions</h3>
              </div>
              <span className="chip" data-tone={workspace.state.week?.conflicts.length ? "warm" : "accent"}>
                {workspace.state.week?.conflicts.length ?? 0}
              </span>
            </div>
            {workspace.state.week?.conflicts.length ? (
              <div className="listStack">
                {workspace.state.week.conflicts.map((conflict) => (
                  <div key={`${conflict.date}-${conflict.task_ids.join("-")}`} className="listRow">
                    <div>
                      <p className="listTitle">{formatDate(conflict.date)}</p>
                      <p className="listSubtitle">{conflict.titles.join(" overlaps ")}</p>
                    </div>
                    <span className="chip" data-tone="warm">
                      conflict
                    </span>
                  </div>
                ))}
              </div>
            ) : (
              <div className="emptyState">
                <p className="emptyStateTitle">No current overlaps</p>
                <p className="emptyStateText">
                  The backend week snapshot is not reporting any overlapping scheduled tasks.
                </p>
              </div>
            )}
          </article>
        </div>

        {workspace.state.error && (
          <article className="surface">
            <span className="eyebrow">Planner issue</span>
            <p className="errorText">{workspace.state.error}</p>
          </article>
        )}
      </div>
    </PageTemplate>
  );
}
