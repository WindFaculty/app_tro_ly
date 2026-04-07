import { useEffect, useState } from "react";
import { PageTemplate } from "@/components/PageTemplate";
import { useDesktopSession } from "@/features/shell/DesktopSessionContext";
import styles from "./StatusPage.module.css";
import type { DesktopRestoreState } from "@contracts";
import type { HealthResponse } from "@/contracts/backend";
import { checkHealth, getBackendBaseUrlForUi } from "@/services/backendClient";
import {
  getDesktopRestoreState,
  getShellRuntimeState,
  type ShellRuntimeState,
} from "@/services/runtimeHost";

function toneForStatus(status: string): string {
  if (status === "ready" || status === "restored" || status === "persisted") {
    return styles.statusReady;
  }
  if (
    status === "partial" ||
    status === "starting" ||
    status === "defaulted" ||
    status === "recovered" ||
    status === "browser_preview"
  ) {
    return styles.statusPartial;
  }
  return styles.statusError;
}

export function StatusPage() {
  const { sessionState } = useDesktopSession();
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const [shellState, setShellState] = useState<ShellRuntimeState | null>(null);
  const [restoreState, setRestoreState] = useState<DesktopRestoreState | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>("");
  const [backendUrl, setBackendUrl] = useState<string>("");
  const showHostDiagnostics = sessionState?.preferences.show_host_diagnostics ?? true;

  const refresh = async () => {
    setLoading(true);
    setError("");
    try {
      const [data, url, shell, restore] = await Promise.all([
        checkHealth(),
        getBackendBaseUrlForUi(),
        getShellRuntimeState(),
        getDesktopRestoreState(),
      ]);
      setHealth(data);
      setBackendUrl(url);
      setShellState(shell);
      setRestoreState(restore);
    } catch (loadError) {
      setError(String(loadError));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    refresh();
  }, []);

  return (
    <PageTemplate
      title="System Status"
      icon="HX"
      eyebrow="Diagnostics lane"
      description="Host health, backend health, and persistence diagnostics now share the same module shell instead of living as raw debug rows."
      highlights={[
        {
          label: "Host",
          value: shellState?.backend_status ?? "...",
          detail: shellState?.runtime_mode ?? "desktop shell",
        },
        {
          label: "Backend",
          value: health?.status ?? "...",
          detail: backendUrl || "Runtime backend URL",
        },
        {
          label: "Restore",
          value: restoreState?.restore_status ?? "...",
          detail: restoreState?.session.active_route ?? "No route restored yet",
        },
      ]}
      actions={
        <button className="secondaryButton" onClick={refresh}>
          Refresh diagnostics
        </button>
      }
    >
      <div className={styles.container}>
        {loading && <p className={styles.loading}>Checking runtime diagnostics...</p>}

        {!loading && error && (
          <article className={styles.errorBox}>
            <p>Could not reach the runtime surfaces.</p>
            <p className={styles.errorDetail}>{error}</p>
          </article>
        )}

        {!loading && !showHostDiagnostics && (
          <article className={styles.healthCard}>
            <div className={styles.cardHeader}>
              <div>
                <span className="eyebrow">Host diagnostics</span>
                <h3 className="surfaceTitle">Detailed host panels are hidden</h3>
              </div>
              <span className={`${styles.badge} ${styles.statusPartial}`}>hidden</span>
            </div>
            <p className={styles.value}>
              Turn host diagnostics back on from Settings if you want shell restore files, host
              paths, and window-state details in this lane.
            </p>
          </article>
        )}

        {!loading && showHostDiagnostics && shellState && (
          <article className={styles.healthCard}>
            <div className={styles.cardHeader}>
              <div>
                <span className="eyebrow">Shell runtime</span>
                <h3 className="surfaceTitle">Desktop host facts</h3>
              </div>
              <span className={`${styles.badge} ${toneForStatus(shellState.backend_status)}`}>
                {shellState.backend_status}
              </span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Shell</span>
              <span className={styles.value}>
                {shellState.app_name} {shellState.app_version}
              </span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Mode</span>
              <span className={styles.value}>{shellState.runtime_mode}</span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Message</span>
              <span className={styles.value}>{shellState.backend_message}</span>
            </div>
            {shellState.app_data_dir && (
              <div className={styles.row}>
                <span className={styles.label}>App data</span>
                <span className={styles.value}>{shellState.app_data_dir}</span>
              </div>
            )}
            {shellState.app_log_dir && (
              <div className={styles.row}>
                <span className={styles.label}>Logs</span>
                <span className={styles.value}>{shellState.app_log_dir}</span>
              </div>
            )}
            {shellState.backend_path && (
              <div className={styles.row}>
                <span className={styles.label}>Backend root</span>
                <span className={styles.value}>{shellState.backend_path}</span>
              </div>
            )}
            {shellState.python_path && (
              <div className={styles.row}>
                <span className={styles.label}>Python</span>
                <span className={styles.value}>{shellState.python_path}</span>
              </div>
            )}
            {shellState.backend_pid && (
              <div className={styles.row}>
                <span className={styles.label}>PID</span>
                <span className={styles.value}>{shellState.backend_pid}</span>
              </div>
            )}
          </article>
        )}

        {!loading && health && (
          <article className={styles.healthCard}>
            <div className={styles.cardHeader}>
              <div>
                <span className="eyebrow">Backend health</span>
                <h3 className="surfaceTitle">Live backend snapshot</h3>
              </div>
              <span className={`${styles.badge} ${toneForStatus(health.status)}`}>{health.status}</span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>URL</span>
              <span className={styles.value}>{backendUrl}</span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Service</span>
              <span className={styles.value}>{health.service}</span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Version</span>
              <span className={styles.value}>{health.version}</span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Degraded</span>
              <span className={styles.value}>
                {health.degraded_features.join(", ") || "No degraded features reported"}
              </span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Recovery</span>
              <span className={styles.value}>
                {health.recovery_actions.join(", ") || "No recovery actions reported"}
              </span>
            </div>
          </article>
        )}

        {!loading && showHostDiagnostics && restoreState && (
          <article className={styles.healthCard}>
            <div className={styles.cardHeader}>
              <div>
                <span className="eyebrow">Desktop restore</span>
                <h3 className="surfaceTitle">Persistence diagnostics</h3>
              </div>
              <span className={`${styles.badge} ${toneForStatus(restoreState.restore_status)}`}>
                {restoreState.restore_status}
              </span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Message</span>
              <span className={styles.value}>{restoreState.restore_message}</span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Last route</span>
              <span className={styles.value}>{restoreState.session.active_route}</span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Recent routes</span>
              <span className={styles.value}>{restoreState.session.recent_routes.join(", ")}</span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Theme</span>
              <span className={styles.value}>{restoreState.theme.active_theme}</span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Planner view</span>
              <span className={styles.value}>{restoreState.filters.planner_view}</span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Snapshot</span>
              <span className={styles.value}>
                {restoreState.runtime_snapshot.backend_status} |{" "}
                {restoreState.runtime_snapshot.unity_runtime_state ?? "unity_unknown"} |{" "}
                {restoreState.runtime_snapshot.unity_bridge_state ?? "bridge_unknown"}
              </span>
            </div>
            {restoreState.paths.session_state && (
              <div className={styles.row}>
                <span className={styles.label}>Session file</span>
                <span className={styles.value}>{restoreState.paths.session_state}</span>
              </div>
            )}
            {restoreState.paths.window_state && (
              <div className={styles.row}>
                <span className={styles.label}>Window file</span>
                <span className={styles.value}>{restoreState.paths.window_state}</span>
              </div>
            )}
            {restoreState.paths.runtime_snapshot && (
              <div className={styles.row}>
                <span className={styles.label}>Snapshot file</span>
                <span className={styles.value}>{restoreState.paths.runtime_snapshot}</span>
              </div>
            )}
          </article>
        )}

        <div className={styles.legend}>
          <div className={styles.legendItem}>
            <span className={`${styles.dot} ${styles.statusReady}`} />
            <span>ready means the lane is healthy or persisted cleanly</span>
          </div>
          <div className={styles.legendItem}>
            <span className={`${styles.dot} ${styles.statusPartial}`} />
            <span>partial means degraded, recovering, or defaulted state</span>
          </div>
          <div className={styles.legendItem}>
            <span className={`${styles.dot} ${styles.statusError}`} />
            <span>error means the shell needs intervention before normal use</span>
          </div>
        </div>
      </div>
    </PageTemplate>
  );
}
