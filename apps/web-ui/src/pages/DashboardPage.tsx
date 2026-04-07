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
  const inboxCount = state.inbox?.count ?? 0;

  return (
    <PageTemplate title="Dashboard" icon="⬡">
      <div className="stack">
        <div className="card">
          <p className="eyebrow">Current implementation</p>
          <h3 className="sectionTitle">React shell là owner của business UI mới</h3>
          <p className="bodyText">
            Dashboard này đang lấy dữ liệu thật từ backend hiện có để chứng minh boundary
            React ↔ FastAPI chạy độc lập với Unity shell cũ.
          </p>
        </div>

        <div className="kpiGrid">
          <div className="card">
            <p className="eyebrow">Backend</p>
            <p className="kpiValue">{state.health?.status ?? "..."}</p>
            <p className="helperText">{state.health?.service ?? "Chưa đọc được health"}</p>
          </div>
          <div className="card">
            <p className="eyebrow">Today</p>
            <p className="kpiValue">{todayCount}</p>
            <p className="helperText">Task trong ngày hiện tại</p>
          </div>
          <div className="card">
            <p className="eyebrow">Due Soon</p>
            <p className="kpiValue">{dueSoonCount}</p>
            <p className="helperText">Task sắp đến hạn</p>
          </div>
          <div className="card">
            <p className="eyebrow">Inbox</p>
            <p className="kpiValue">{inboxCount}</p>
            <p className="helperText">Task chưa được schedule</p>
          </div>
        </div>

        {state.error && (
          <div className="card">
            <p className="errorText">Không đọc được dữ liệu dashboard: {state.error}</p>
          </div>
        )}

        {state.today && state.today.items.length > 0 && (
          <div className="card">
            <h3 className="sectionTitle">Việc gần nhất</h3>
            <div className="listStack">
              {state.today.items.slice(0, 4).map((task) => (
                <div key={task.id} className="listRow">
                  <div>
                    <p className="listTitle">{task.title}</p>
                    <p className="helperText">
                      {task.scheduled_date ?? "unscheduled"} · {task.priority}
                    </p>
                  </div>
                  <span className="pill">{task.status}</span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </PageTemplate>
  );
}
