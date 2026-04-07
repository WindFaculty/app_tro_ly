import { useEffect, useState } from "react";
import { PageTemplate } from "@/components/PageTemplate";
import type { HealthResponse, SettingsResponse } from "@/contracts/backend";
import { useDesktopSession } from "@/features/shell/DesktopSessionContext";
import { checkHealth, getSettings, resetSettings, updateSettings } from "@/services/backendClient";
import {
  getDesktopRestoreState,
  getShellRuntimeState,
  resetDesktopRestoreState,
  restartDesktopBackend,
  type DesktopRestoreState,
  type ShellRuntimeState,
} from "@/services/runtimeHost";
import styles from "./SettingsPage.module.css";

function toneForStatus(status: string | undefined): "accent" | "sun" | "success" | "danger" {
  if (status === "ready" || status === "persisted" || status === "reset") {
    return "success";
  }
  if (status === "partial" || status === "restored" || status === "defaulted" || status === "recovered") {
    return "sun";
  }
  if (status === "error" || status === "timeout") {
    return "danger";
  }
  return "accent";
}

function formatList(items: string[] | undefined, fallback: string): string {
  return items && items.length > 0 ? items.join(", ") : fallback;
}

function formatPath(value: string | null | undefined): string {
  const normalized = value?.trim();
  return normalized && normalized.length > 0 ? normalized : "not available";
}

export function SettingsPage() {
  const { sessionState, updateSessionState } = useDesktopSession();
  const [settings, setSettings] = useState<SettingsResponse | null>(null);
  const [draft, setDraft] = useState<SettingsResponse | null>(null);
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const [shellState, setShellState] = useState<ShellRuntimeState | null>(null);
  const [restoreState, setRestoreState] = useState<DesktopRestoreState | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState("");
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const refresh = async () => {
    setBusy("refresh");
    setError("");
    try {
      const [nextSettings, nextHealth, nextShell, nextRestore] = await Promise.all([
        getSettings(),
        checkHealth(),
        getShellRuntimeState(),
        getDesktopRestoreState(),
      ]);
      setSettings(nextSettings);
      setDraft({
        voice: { ...nextSettings.voice },
        model: { ...nextSettings.model },
        window_mode: { ...nextSettings.window_mode },
        avatar: { ...nextSettings.avatar },
        reminder: { ...nextSettings.reminder },
        startup: { ...nextSettings.startup },
        memory: { ...nextSettings.memory },
      });
      setHealth(nextHealth);
      setShellState(nextShell);
      setRestoreState(nextRestore);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : String(loadError));
    } finally {
      setBusy("");
      setLoading(false);
    }
  };

  useEffect(() => {
    void refresh();
  }, []);

  const currentTheme = sessionState?.theme.active_theme ?? restoreState?.theme.active_theme ?? "system";
  const restoreLastRoute =
    sessionState?.preferences.restore_last_route ?? restoreState?.preferences.restore_last_route ?? true;
  const showHostDiagnostics =
    sessionState?.preferences.show_host_diagnostics ??
    restoreState?.preferences.show_host_diagnostics ??
    true;
  const plannerView = sessionState?.filters.planner_view ?? restoreState?.filters.planner_view ?? "today";
  const unsaved =
    settings !== null &&
    draft !== null &&
    (settings.voice.tts_voice !== draft.voice.tts_voice ||
      settings.voice.speak_replies !== draft.voice.speak_replies ||
      settings.reminder.lead_minutes !== draft.reminder.lead_minutes ||
      settings.reminder.speech_enabled !== draft.reminder.speech_enabled ||
      settings.memory.auto_extract !== draft.memory.auto_extract ||
      settings.memory.short_term_turn_limit !== draft.memory.short_term_turn_limit);

  const pathRows = [
    { label: "Desktop app data", value: shellState?.app_data_dir ?? null },
    { label: "Desktop cache", value: shellState?.app_cache_dir ?? null },
    { label: "Desktop config", value: shellState?.app_config_dir ?? null },
    { label: "Desktop logs", value: shellState?.app_log_dir ?? null },
    { label: "Desktop exports", value: shellState?.export_dir ?? null },
    { label: "Backend root", value: shellState?.backend_path ?? null },
    { label: "SQLite", value: typeof health?.database.path === "string" ? health.database.path : null },
    {
      label: "Backend app log",
      value: typeof health?.logs.app_log === "string" ? health.logs.app_log : null,
    },
    { label: "Python", value: shellState?.python_path ?? null },
  ];

  const saveBackendSettings = async () => {
    if (!draft) {
      return;
    }
    setBusy("save");
    setMessage("");
    try {
      const updated = await updateSettings({
        voice: {
          tts_voice: draft.voice.tts_voice.trim() || "vi-VN-default",
          speak_replies: draft.voice.speak_replies,
        },
        reminder: {
          speech_enabled: draft.reminder.speech_enabled,
          lead_minutes: Math.min(Math.max(Number(draft.reminder.lead_minutes) || 15, 1), 180),
        },
        memory: {
          auto_extract: draft.memory.auto_extract,
          short_term_turn_limit: Math.min(
            Math.max(Number(draft.memory.short_term_turn_limit) || 12, 1),
            64,
          ),
        },
      });
      setSettings(updated);
      setDraft({
        voice: { ...updated.voice },
        model: { ...updated.model },
        window_mode: { ...updated.window_mode },
        avatar: { ...updated.avatar },
        reminder: { ...updated.reminder },
        startup: { ...updated.startup },
        memory: { ...updated.memory },
      });
      setHealth(await checkHealth());
      setMessage("Backend-backed settings saved.");
    } catch (saveError) {
      setMessage(saveError instanceof Error ? saveError.message : String(saveError));
    } finally {
      setBusy("");
    }
  };

  const resetBackendDefaults = async () => {
    setBusy("reset_backend");
    setMessage("");
    try {
      const updated = await resetSettings();
      setSettings(updated);
      setDraft({
        voice: { ...updated.voice },
        model: { ...updated.model },
        window_mode: { ...updated.window_mode },
        avatar: { ...updated.avatar },
        reminder: { ...updated.reminder },
        startup: { ...updated.startup },
        memory: { ...updated.memory },
      });
      setHealth(await checkHealth());
      setMessage("Backend settings were reset to defaults.");
    } catch (resetError) {
      setMessage(resetError instanceof Error ? resetError.message : String(resetError));
    } finally {
      setBusy("");
    }
  };

  const resetShellState = async () => {
    setBusy("reset_shell");
    setMessage("");
    try {
      const nextRestore = await resetDesktopRestoreState();
      setRestoreState(nextRestore);
      updateSessionState((current) => ({
        session: {
          ...nextRestore.session,
          active_route: current.session.active_route,
          recent_routes: [current.session.active_route],
        },
        preferences: nextRestore.preferences,
        theme: nextRestore.theme,
        filters: nextRestore.filters,
        runtime_snapshot: {
          ...current.runtime_snapshot,
          ...nextRestore.runtime_snapshot,
        },
      }));
      setMessage("Desktop restore files were reset. Current page stays open.");
    } catch (resetError) {
      setMessage(resetError instanceof Error ? resetError.message : String(resetError));
    } finally {
      setBusy("");
    }
  };

  const requestBackendRestart = async () => {
    setBusy("restart");
    setMessage("");
    try {
      const event = await restartDesktopBackend();
      setShellState(await getShellRuntimeState());
      setMessage(event.message ?? "Backend restart requested.");
    } catch (restartError) {
      setMessage(restartError instanceof Error ? restartError.message : String(restartError));
    } finally {
      setBusy("");
    }
  };

  return (
    <PageTemplate
      title="Settings"
      icon="ST"
      eyebrow="Recovery center"
      description="A14 turns settings into a real workspace for backend preferences, privacy and path visibility, diagnostics, and recovery actions."
      actions={
        <button className="secondaryButton" onClick={() => void refresh()} disabled={busy === "refresh"}>
          {busy === "refresh" ? "Refreshing..." : "Refresh facts"}
        </button>
      }
      highlights={[
        { label: "Backend", value: health?.status ?? "...", detail: health?.service ?? "health" },
        { label: "Theme", value: currentTheme, detail: showHostDiagnostics ? "host details visible" : "host details hidden" },
        { label: "Restore", value: restoreState?.restore_status ?? "...", detail: restoreLastRoute ? "route reopens" : "start fresh" },
        { label: "Voice", value: settings?.voice.tts_voice ?? "...", detail: settings?.voice.speak_replies ? "speech enabled" : "speech disabled" },
      ]}
    >
      <div className="appStack">
        <article className="surface surfaceMuted">
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Current implementation</span>
              <h3 className="surfaceTitle">Settings now use real save and reset surfaces</h3>
              <p className="surfaceIntro">
                Backend values save through <code>PUT /v1/settings</code>, local shell preferences persist through the desktop restore surface, and recovery actions now have reset entry points.
              </p>
            </div>
          </div>
        </article>

        {error && (
          <article className="surface">
            <span className="eyebrow">Data issue</span>
            <p className="errorText">Could not load settings center: {error}</p>
          </article>
        )}

        {!loading && settings && draft ? (
          <>
            <div className={styles.grid}>
              <article className="surface">
                <div className="surfaceHeader">
                  <div className="surfaceHeaderBlock">
                    <span className="eyebrow">Shell preferences</span>
                    <h3 className="surfaceTitle">Local desktop behavior</h3>
                  </div>
                  <span className="chip" data-tone="accent">local</span>
                </div>
                <div className={styles.fields}>
                  <label className={styles.field}>
                    <span className={styles.label}>Theme</span>
                    <span className={styles.hint}>Applies immediately and persists across restarts.</span>
                    <select
                      className="textInput"
                      value={currentTheme}
                      onChange={(event) =>
                        updateSessionState((current) => ({
                          ...current,
                          theme: { ...current.theme, active_theme: event.target.value as typeof current.theme.active_theme },
                        }))
                      }
                    >
                      <option value="system">System</option>
                      <option value="light">Light</option>
                      <option value="dark">Dark</option>
                    </select>
                  </label>
                  <label className={styles.field}>
                    <span className={styles.label}>Planner default</span>
                    <span className={styles.hint}>Persists the planner lens used by the shell.</span>
                    <select
                      className="textInput"
                      value={plannerView}
                      onChange={(event) =>
                        updateSessionState((current) => ({
                          ...current,
                          filters: { ...current.filters, planner_view: event.target.value as typeof current.filters.planner_view },
                        }))
                      }
                    >
                      <option value="today">Today</option>
                      <option value="week">Week</option>
                      <option value="calendar">Calendar</option>
                      <option value="inbox">Inbox</option>
                      <option value="overdue">Overdue</option>
                      <option value="completed">Completed</option>
                      <option value="reminders">Reminders</option>
                      <option value="tags">Tags</option>
                    </select>
                  </label>
                  <label className={styles.field}>
                    <span className={styles.label}>Restore last route</span>
                    <span className={styles.hint}>If disabled, startup ignores the previous page.</span>
                    <input
                      type="checkbox"
                      checked={restoreLastRoute}
                      onChange={(event) =>
                        updateSessionState((current) => ({
                          ...current,
                          preferences: { ...current.preferences, restore_last_route: event.target.checked },
                        }))
                      }
                    />
                  </label>
                  <label className={styles.field}>
                    <span className={styles.label}>Show host diagnostics</span>
                    <span className={styles.hint}>Controls detail visibility in the Status lane.</span>
                    <input
                      type="checkbox"
                      checked={showHostDiagnostics}
                      onChange={(event) =>
                        updateSessionState((current) => ({
                          ...current,
                          preferences: { ...current.preferences, show_host_diagnostics: event.target.checked },
                        }))
                      }
                    />
                  </label>
                </div>
              </article>

              <article className="surface">
                <div className="surfaceHeader">
                  <div className="surfaceHeaderBlock">
                    <span className="eyebrow">Backend settings</span>
                    <h3 className="surfaceTitle">Voice, reminder, and memory defaults</h3>
                  </div>
                  <span className="chip" data-tone={unsaved ? "warm" : "success"}>
                    {unsaved ? "unsaved" : "synced"}
                  </span>
                </div>
                <div className={styles.fields}>
                  <label className={styles.field}>
                    <span className={styles.label}>TTS voice</span>
                    <span className={styles.hint}>Used for replies and reminder speech.</span>
                    <input
                      className="textInput"
                      type="text"
                      value={draft.voice.tts_voice}
                      onChange={(event) =>
                        setDraft((current) =>
                          current ? { ...current, voice: { ...current.voice, tts_voice: event.target.value } } : current,
                        )
                      }
                    />
                  </label>
                  <label className={styles.field}>
                    <span className={styles.label}>Speak replies</span>
                    <span className={styles.hint}>Keeps text replies while disabling auto speech.</span>
                    <input
                      type="checkbox"
                      checked={draft.voice.speak_replies}
                      onChange={(event) =>
                        setDraft((current) =>
                          current ? { ...current, voice: { ...current.voice, speak_replies: event.target.checked } } : current,
                        )
                      }
                    />
                  </label>
                  <label className={styles.field}>
                    <span className={styles.label}>Reminder speech</span>
                    <span className={styles.hint}>Lets due reminders attempt speech output.</span>
                    <input
                      type="checkbox"
                      checked={draft.reminder.speech_enabled}
                      onChange={(event) =>
                        setDraft((current) =>
                          current ? { ...current, reminder: { ...current.reminder, speech_enabled: event.target.checked } } : current,
                        )
                      }
                    />
                  </label>
                  <label className={styles.field}>
                    <span className={styles.label}>Lead minutes</span>
                    <span className={styles.hint}>How early reminders should fire.</span>
                    <input
                      className="textInput"
                      type="number"
                      min={1}
                      max={180}
                      value={draft.reminder.lead_minutes}
                      onChange={(event) =>
                        setDraft((current) =>
                          current ? { ...current, reminder: { ...current.reminder, lead_minutes: Number(event.target.value) } } : current,
                        )
                      }
                    />
                  </label>
                  <label className={styles.field}>
                    <span className={styles.label}>Auto extract memory</span>
                    <span className={styles.hint}>Keeps memory candidate extraction active.</span>
                    <input
                      type="checkbox"
                      checked={draft.memory.auto_extract}
                      onChange={(event) =>
                        setDraft((current) =>
                          current ? { ...current, memory: { ...current.memory, auto_extract: event.target.checked } } : current,
                        )
                      }
                    />
                  </label>
                  <label className={styles.field}>
                    <span className={styles.label}>Short-term turn limit</span>
                    <span className={styles.hint}>Caps rolling short-term context.</span>
                    <input
                      className="textInput"
                      type="number"
                      min={1}
                      max={64}
                      value={draft.memory.short_term_turn_limit}
                      onChange={(event) =>
                        setDraft((current) =>
                          current ? { ...current, memory: { ...current.memory, short_term_turn_limit: Number(event.target.value) } } : current,
                        )
                      }
                    />
                  </label>
                </div>
                <div className="actionRow">
                  <span className="helperText">{message || "Current implementation keeps provider routing read-only in React."}</span>
                  <button className="primaryButton" onClick={() => void saveBackendSettings()} disabled={busy === "save" || !unsaved}>
                    {busy === "save" ? "Saving..." : "Save backend settings"}
                  </button>
                </div>
              </article>
            </div>

            <div className={styles.grid}>
              <article className="surface">
                <div className="surfaceHeader">
                  <div className="surfaceHeaderBlock">
                    <span className="eyebrow">Privacy and paths</span>
                    <h3 className="surfaceTitle">Local storage visibility</h3>
                  </div>
                </div>
                <div className={styles.pathList}>
                  {pathRows.map((item) => (
                    <div key={item.label} className={styles.pathRow}>
                      <span className={styles.pathLabel}>{item.label}</span>
                      <code className={styles.pathValue}>{formatPath(item.value)}</code>
                    </div>
                  ))}
                </div>
                <p className="helperText">
                  Provider secrets still belong to backend environment configuration, not React page state.
                </p>
              </article>

              <article className="surface surfaceMuted">
                <div className="surfaceHeader">
                  <div className="surfaceHeaderBlock">
                    <span className="eyebrow">Diagnostics</span>
                    <h3 className="surfaceTitle">Health, provider, and restore facts</h3>
                  </div>
                  <span className="chip" data-tone={toneForStatus(health?.status)}>{health?.status ?? "checking"}</span>
                </div>
                <div className="detailGrid">
                  <div className="detailRow">
                    <span className="detailLabel">Provider</span>
                    <span className="detailValue">{settings.model.provider}</span>
                  </div>
                  <div className="detailRow">
                    <span className="detailLabel">Model</span>
                    <span className="detailValue">{settings.model.name}</span>
                  </div>
                  <div className="detailRow">
                    <span className="detailLabel">Degraded</span>
                    <span className="detailValue">{formatList(health?.degraded_features, "No degraded features reported")}</span>
                  </div>
                  <div className="detailRow">
                    <span className="detailLabel">Recovery</span>
                    <span className="detailValue">{formatList(health?.recovery_actions, "No recovery actions reported")}</span>
                  </div>
                  <div className="detailRow">
                    <span className="detailLabel">Last route</span>
                    <span className="detailValue">{restoreState?.session.active_route ?? "/"}</span>
                  </div>
                </div>
              </article>
            </div>

            <article className="surface surfaceHero">
              <div className="surfaceHeader">
                <div className="surfaceHeaderBlock">
                  <span className="eyebrow">Recovery center</span>
                  <h3 className="surfaceTitle">Safe retry and reset actions</h3>
                </div>
              </div>
              <div className={styles.grid}>
                <div className={styles.recoveryCard}>
                  <p className={styles.recoveryLabel}>Backend settings</p>
                  <p className={styles.recoveryText}>Reset saved backend preferences back to service defaults.</p>
                  <button className="ghostButton" onClick={() => void resetBackendDefaults()} disabled={busy === "reset_backend"}>
                    {busy === "reset_backend" ? "Resetting..." : "Reset backend defaults"}
                  </button>
                </div>
                <div className={styles.recoveryCard}>
                  <p className={styles.recoveryLabel}>Desktop restore</p>
                  <p className={styles.recoveryText}>Clear host restore files and rebuild them with defaults.</p>
                  <button className="ghostButton" onClick={() => void resetShellState()} disabled={busy === "reset_shell"}>
                    {busy === "reset_shell" ? "Resetting..." : "Reset shell state"}
                  </button>
                </div>
                <div className={styles.recoveryCard}>
                  <p className={styles.recoveryLabel}>Backend lifecycle</p>
                  <p className={styles.recoveryText}>Request a fresh backend start through the host lifecycle surface.</p>
                  <button className="secondaryButton" onClick={() => void requestBackendRestart()} disabled={busy === "restart"}>
                    {busy === "restart" ? "Restarting..." : "Restart backend"}
                  </button>
                </div>
              </div>
            </article>
          </>
        ) : (
          !error && (
            <div className="emptyState">
              <p className="emptyStateTitle">Loading settings center</p>
              <p className="emptyStateText">Reading backend settings, host restore state, and diagnostics.</p>
            </div>
          )
        )}
      </div>
    </PageTemplate>
  );
}
