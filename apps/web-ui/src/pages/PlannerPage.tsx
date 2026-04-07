import { useEffect, useState } from "react";
import { PageTemplate } from "@/components/PageTemplate";
import type { DayTasksResponse, TaskListResponse, WeekTasksResponse } from "@/contracts/backend";
import {
  getCompletedTasks,
  getInboxTasks,
  getTodayTasks,
  getWeekTasks,
} from "@/services/backendClient";

interface PlannerState {
  today: DayTasksResponse | null;
  week: WeekTasksResponse | null;
  inbox: TaskListResponse | null;
  completed: TaskListResponse | null;
  error: string;
}

export function PlannerPage() {
  const [state, setState] = useState<PlannerState>({
    today: null,
    week: null,
    inbox: null,
    completed: null,
    error: "",
  });

  useEffect(() => {
    let cancelled = false;

    Promise.all([getTodayTasks(), getWeekTasks(), getInboxTasks(), getCompletedTasks()])
      .then(([today, week, inbox, completed]) => {
        if (!cancelled) {
          setState({ today, week, inbox, completed, error: "" });
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

  return (
    <PageTemplate title="Planner" icon="📅">
      <div className="stack">
        <div className="kpiGrid">
          <div className="card">
            <p className="eyebrow">Today</p>
            <p className="kpiValue">{state.today?.items.length ?? "..."}</p>
          </div>
          <div className="card">
            <p className="eyebrow">Overdue</p>
            <p className="kpiValue">{state.today?.overdue.length ?? "..."}</p>
          </div>
          <div className="card">
            <p className="eyebrow">Inbox</p>
            <p className="kpiValue">{state.inbox?.count ?? "..."}</p>
          </div>
          <div className="card">
            <p className="eyebrow">Completed</p>
            <p className="kpiValue">{state.completed?.count ?? "..."}</p>
          </div>
        </div>

        {state.error && (
          <div className="card">
            <p className="errorText">Không đọc được planner data: {state.error}</p>
          </div>
        )}

        {state.week && (
          <div className="card">
            <div className="listRow">
              <div>
                <h3 className="sectionTitle">Week summary</h3>
                <p className="helperText">
                  {state.week.start_date} → {state.week.end_date}
                </p>
              </div>
              <span className="pill">
                {state.week.conflicts.length} conflict{state.week.conflicts.length === 1 ? "" : "s"}
              </span>
            </div>
            <div className="listStack">
              {state.week.days.slice(0, 7).map((day) => (
                <div key={day.date} className="listRow">
                  <div>
                    <p className="listTitle">{day.date}</p>
                    <p className="helperText">
                      {day.task_count} task · {day.high_priority_count} high priority
                    </p>
                  </div>
                  <span className="pill">{day.items.length}</span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </PageTemplate>
  );
}
