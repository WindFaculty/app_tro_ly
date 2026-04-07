import { useEffect, useState } from "react";
import { PageTemplate } from "@/components/PageTemplate";
import type { DayTasksResponse, HealthResponse, TaskListResponse } from "@/contracts/backend";
import { checkHealth, getInboxTasks, getTodayTasks } from "@/services/backendClient";

interface DashboardState {
  health: HealthResponse | null;
  today: DayTasksResponse | null;
  inbox: TaskListResponse | null;
  error: string;
}

function toneForHealth(status: string | undefined): "success" | "sun" | "danger" {
  if (status === "ready") {
    return "success";
  }
  if (status === "partial" || status === "starting") {
    return "sun";
  }
  return "danger";
}

export function DashboardPage() {
  const [state, setState] = useState<DashboardState>({
    health: null,
    today: null,
    inbox: null,
    error: "",
  });

  useEffect(() => {
    let cancelled = false;

    Promise.all([checkHealth(), getTodayTasks(), getInboxTasks()])
      .then(([health, today, inbox]) => {
        if (!cancelled) {
          setState({ health, today, inbox, error: "" });
        }
      })
      .catch((error: unknown) => {
        if (!cancelled) {
          setState((current) => ({
            ...current,
            error: error instanceof Error ? error.message : String(error),
          }));
        }
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const todayCount = state.today?.items.length ?? 0;
  const dueSoonCount = state.today?.due_soon.length ?? 0;
  const overdueCount = state.today?.overdue.length ?? 0;
  const inboxCount = state.inbox?.count ?? 0;
  const inProgressCount = state.today?.in_progress.length ?? 0;
  const firstTasks = state.today?.items.slice(0, 4) ?? [];

  return (
    <PageTemplate
      title="Dashboard"
      icon="DS"
      eyebrow="Shell overview"
      description="Daily signal, backend readiness, and the most actionable task context now share one design language instead of separate scaffolds."
      highlights={[
        {
          label: "Backend",
          value: state.health?.status ?? "...",
          detail: state.health?.service ?? "Waiting for runtime health",
        },
        {
          label: "Today",
          value: String(todayCount),
          detail: `${inProgressCount} in progress now`,
        },
        {
          label: "Due soon",
          value: String(dueSoonCount),
          detail: `${overdueCount} overdue`,
        },
        {
          label: "Inbox",
          value: String(inboxCount),
          detail: "Unscheduled tasks",
        },
      ]}
    >
      <div className="appStack">
        <article className="surface">
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Current implementation</span>
              <h3 className="surfaceTitle">React is now the business shell owner</h3>
              <p className="surfaceIntro">
                Dashboard pulls live backend data and frames it in the A05 design system so later
                modules can build on a stable shell language instead of one-off cards.
              </p>
            </div>
            <span className="chip" data-tone={toneForHealth(state.health?.status)}>
              {state.health?.status ?? "checking"}
            </span>
          </div>
        </article>

        <div className="metricGrid">
          <article className="metricCard">
            <span className="metricLabel">In progress</span>
            <strong className="metricValue">{inProgressCount}</strong>
            <span className="metricHint">Tasks actively moving today</span>
          </article>
          <article className="metricCard">
            <span className="metricLabel">Overdue</span>
            <strong className="metricValue">{overdueCount}</strong>
            <span className="metricHint">Needs attention before planning ahead</span>
          </article>
        </div>

        {state.error && (
          <article className="surface">
            <span className="eyebrow">Data issue</span>
            <p className="errorText">Could not load dashboard data: {state.error}</p>
          </article>
        )}

        <article className="surface">
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Focus list</span>
              <h3 className="surfaceTitle">Nearest tasks</h3>
            </div>
            <div className="chipRow">
              <span className="chip" data-tone="accent">
                today {todayCount}
              </span>
              <span className="chip" data-tone="warm">
                inbox {inboxCount}
              </span>
            </div>
          </div>

          {firstTasks.length > 0 ? (
            <div className="listStack">
              {firstTasks.map((task) => (
                <div key={task.id} className="listRow">
                  <div>
                    <p className="listTitle">{task.title}</p>
                    <p className="listSubtitle">
                      {task.scheduled_date ?? "unscheduled"} | {task.priority} | {task.status}
                    </p>
                  </div>
                  <span className="chip" data-tone={task.status === "completed" ? "success" : "accent"}>
                    {task.status}
                  </span>
                </div>
              ))}
            </div>
          ) : (
            <div className="emptyState">
              <p className="emptyStateTitle">No task snapshot yet</p>
              <p className="emptyStateText">
                When the backend returns today items, they will surface here as the shell default
                focus list.
              </p>
            </div>
          )}
        </article>

        <article className="surface surfaceMuted">
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Recovery signal</span>
              <h3 className="surfaceTitle">Health notes</h3>
            </div>
          </div>

          <div className="detailGrid">
            <div className="detailRow">
              <span className="detailLabel">Runtime</span>
              <span className="detailValue">{state.health?.service ?? "not loaded"}</span>
            </div>
            <div className="detailRow">
              <span className="detailLabel">Recovery</span>
              <span className="detailValue">
                {state.health?.recovery_actions.join(", ") || "No recovery actions reported"}
              </span>
            </div>
            <div className="detailRow">
              <span className="detailLabel">Degraded</span>
              <span className="detailValue">
                {state.health?.degraded_features.join(", ") || "No degraded features reported"}
              </span>
            </div>
          </div>
        </article>
      </div>
    </PageTemplate>
  );
}
