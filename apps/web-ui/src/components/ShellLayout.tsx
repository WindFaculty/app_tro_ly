import { useEffect, useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import type {
  BackendLifecycleEvent,
  DesktopRestoreState,
  DesktopSessionState,
  UnityBridgeEvent,
  UnityBridgeStatus,
} from "@contracts";
import styles from "./ShellLayout.module.css";
import { WindowChrome } from "./WindowChrome";
import {
  getShellRuntimeState,
  getUnityRuntimeStatus,
  launchUnityRuntime,
  persistDesktopSessionState,
  stopUnityRuntime,
  subscribeUnityRuntimeStatus,
  type RuntimeMode,
  type UnityRuntimeStatus,
} from "@/services/runtimeHost";
import {
  getUnityBridgeStatus,
  pathToAppPage,
  sendPageChangedCommand,
  subscribeUnityBridgeEvents,
  subscribeUnityBridgeStatus,
} from "@/services/unityBridge";
import { DesktopSessionContext } from "@/features/shell/DesktopSessionContext";

const NAV_ITEMS = [
  { to: "/", label: "Dashboard", icon: "01", hint: "Daily signal" },
  { to: "/chat", label: "Chat", icon: "02", hint: "Assistant lane" },
  { to: "/planner", label: "Planner", icon: "03", hint: "Work rhythm" },
  { to: "/notes", label: "Notes", icon: "04", hint: "Knowledge graph" },
  { to: "/wardrobe", label: "Wardrobe", icon: "05", hint: "Taxonomy shell" },
  { to: "/settings", label: "Settings", icon: "06", hint: "Preferences" },
  { to: "/status", label: "Status", icon: "07", hint: "Diagnostics" },
];

interface ShellLayoutProps {
  runtimeMode: RuntimeMode;
  backendEvent?: BackendLifecycleEvent | null;
  onRetryBackend?: () => void | Promise<void>;
  retrying?: boolean;
  initialRestoreState?: DesktopRestoreState | null;
}

function restoreStateToSessionState(
  restoreState: DesktopRestoreState | null | undefined,
): DesktopSessionState | null {
  if (!restoreState) {
    return null;
  }

  return {
    session: restoreState.session,
    preferences: restoreState.preferences,
    theme: restoreState.theme,
    filters: restoreState.filters,
    runtime_snapshot: restoreState.runtime_snapshot,
  };
}

function toneForStatus(status: string | undefined): "accent" | "sun" | "success" | "danger" | "warm" {
  switch (status) {
    case "ready":
    case "running":
    case "connected":
    case "persisted":
      return "success";
    case "error":
    case "timeout":
    case "failed":
    case "disconnected":
      return "danger";
    case "starting":
    case "checking":
    case "partial":
    case "defaulted":
    case "recovered":
    case "browser_preview":
      return "sun";
    case "ready_to_launch":
    case "idle":
      return "accent";
    default:
      return "warm";
  }
}

function routeLabel(pathname: string): string {
  if (pathname === "/") {
    return "dashboard";
  }

  return pathname.replace(/^\//, "");
}

export function ShellLayout({
  runtimeMode,
  backendEvent = null,
  onRetryBackend,
  retrying = false,
  initialRestoreState = null,
}: ShellLayoutProps) {
  const location = useLocation();
  const [unityStatus, setUnityStatus] = useState<UnityRuntimeStatus | null>(null);
  const [bridgeStatus, setBridgeStatus] = useState<UnityBridgeStatus | null>(null);
  const [lastBridgeEvent, setLastBridgeEvent] = useState<UnityBridgeEvent | null>(null);
  const [unityBusy, setUnityBusy] = useState(false);
  const [sessionState, setSessionState] = useState<DesktopSessionState | null>(
    restoreStateToSessionState(initialRestoreState),
  );

  useEffect(() => {
    setSessionState(restoreStateToSessionState(initialRestoreState));
  }, [initialRestoreState]);

  useEffect(() => {
    const root = document.documentElement;
    const theme = sessionState?.theme.active_theme ?? "system";
    const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");

    const applyTheme = () => {
      const resolved = theme === "system" ? (mediaQuery.matches ? "dark" : "light") : theme;
      root.dataset.theme = resolved;
    };

    applyTheme();
    mediaQuery.addEventListener("change", applyTheme);

    return () => {
      mediaQuery.removeEventListener("change", applyTheme);
    };
  }, [sessionState?.theme.active_theme]);

  useEffect(() => {
    let mounted = true;

    getUnityRuntimeStatus().then((status) => {
      if (mounted) {
        setUnityStatus(status);
      }
    });

    const subscription = subscribeUnityRuntimeStatus((status) => {
      if (mounted) {
        setUnityStatus(status);
      }
    });

    return () => {
      mounted = false;
      subscription.then((unlisten) => unlisten()).catch(() => undefined);
    };
  }, []);

  useEffect(() => {
    let mounted = true;

    getUnityBridgeStatus().then((status) => {
      if (mounted) {
        setBridgeStatus(status);
      }
    });

    const statusSubscription = subscribeUnityBridgeStatus((status) => {
      if (mounted) {
        setBridgeStatus(status);
      }
    });
    const eventSubscription = subscribeUnityBridgeEvents((event) => {
      if (mounted) {
        setLastBridgeEvent(event);
      }
    });

    return () => {
      mounted = false;
      statusSubscription.then((unlisten) => unlisten()).catch(() => undefined);
      eventSubscription.then((unlisten) => unlisten()).catch(() => undefined);
    };
  }, []);

  useEffect(() => {
    const page = pathToAppPage(location.pathname);
    sendPageChangedCommand(page)
      .then((result) => {
        setBridgeStatus(result.status);
      })
      .catch(() => undefined);
  }, [location.pathname]);

  useEffect(() => {
    if (!sessionState) {
      return;
    }

    let cancelled = false;

    const persist = async () => {
      const shellState = await getShellRuntimeState();
      if (cancelled) {
        return;
      }

      const persisted = await persistDesktopSessionState({
        session: {
          ...sessionState.session,
          active_route: location.pathname,
          recent_routes: [location.pathname, ...sessionState.session.recent_routes],
        },
        preferences: sessionState.preferences,
        theme: sessionState.theme,
        filters: sessionState.filters,
        runtime_snapshot: {
          ...sessionState.runtime_snapshot,
          runtime_mode: shellState.runtime_mode,
          backend_status: backendEvent?.status ?? shellState.backend_status,
          backend_message: backendEvent?.message ?? shellState.backend_message,
          backend_ready: shellState.backend_ready,
          unity_runtime_state: unityStatus?.state ?? null,
          unity_bridge_state: bridgeStatus?.state ?? null,
          window_maximized: shellState.window_maximized,
        },
      });

      if (!cancelled) {
        setSessionState(restoreStateToSessionState(persisted));
      }
    };

    persist().catch(() => undefined);

    return () => {
      cancelled = true;
    };
  }, [
    runtimeMode,
    location.pathname,
    backendEvent?.status,
    backendEvent?.message,
    unityStatus?.state,
    bridgeStatus?.state,
    sessionState?.preferences.restore_last_route,
    sessionState?.preferences.show_host_diagnostics,
    sessionState?.theme.active_theme,
    sessionState?.filters.planner_view,
  ]);

  const refreshUnityStatus = async () => {
    setUnityBusy(true);
    try {
      setUnityStatus(await getUnityRuntimeStatus());
      setBridgeStatus(await getUnityBridgeStatus());
    } finally {
      setUnityBusy(false);
    }
  };

  const startUnitySidecar = async () => {
    setUnityBusy(true);
    try {
      setUnityStatus(await launchUnityRuntime());
      setBridgeStatus(await getUnityBridgeStatus());
    } finally {
      setUnityBusy(false);
    }
  };

  const stopUnitySidecar = async () => {
    setUnityBusy(true);
    try {
      setUnityStatus(await stopUnityRuntime());
      setBridgeStatus(await getUnityBridgeStatus());
    } finally {
      setUnityBusy(false);
    }
  };

  const canLaunch = runtimeMode === "desktop" && unityStatus?.state === "ready_to_launch";
  const canStop = runtimeMode === "desktop" && unityStatus?.state === "running";
  const backendStatus = backendEvent?.status ?? sessionState?.runtime_snapshot.backend_status ?? runtimeMode;
  const backendMessage =
    backendEvent?.message ??
    sessionState?.runtime_snapshot.backend_message ??
    (runtimeMode === "desktop" ? "Desktop shell is standing by." : "Browser preview mode.");
  const restoreRoute = sessionState?.session.active_route ?? initialRestoreState?.session.active_route ?? "/";
  const recentRoutes = sessionState?.session.recent_routes ?? initialRestoreState?.session.recent_routes ?? ["/"];
  const restoreStatus = initialRestoreState?.restore_status ?? "defaulted";
  const updateSessionState = (updater: (current: DesktopSessionState) => DesktopSessionState) => {
    setSessionState((current) => {
      if (!current) {
        return current;
      }

      return updater(current);
    });
  };

  return (
    <div className={styles.shell}>
      <WindowChrome
        runtimeMode={runtimeMode}
        backendEvent={backendEvent}
        onRetryBackend={backendEvent?.status === "error" ? onRetryBackend : undefined}
        retryBusy={retrying}
        compact
      />

      <div className={styles.body}>
        <nav className={styles.sideNav}>
          <div className={styles.brandCard}>
            <span className={styles.brandMark}>A</span>
            <div className={styles.brandCopy}>
              <span className={styles.brandEyebrow}>Desktop rebuild</span>
              <strong className={styles.brandTitle}>App Tro Ly</strong>
              <span className={styles.brandLead}>
                React now owns the shell while Unity stays optional during Workstream A.
              </span>
            </div>
          </div>

          <div className={styles.navSection}>
            <span className={styles.navSectionLabel}>Workspace</span>
            <ul className={styles.navList}>
              {NAV_ITEMS.map((item) => (
                <li key={item.to}>
                  <NavLink
                    to={item.to}
                    end={item.to === "/"}
                    className={({ isActive }) =>
                      `${styles.navItem} ${isActive ? styles.navItemActive : ""}`
                    }
                  >
                    <span className={styles.navIcon}>{item.icon}</span>
                    <span className={styles.navCopy}>
                      <span className={styles.navLabel}>{item.label}</span>
                      <span className={styles.navHint}>{item.hint}</span>
                    </span>
                  </NavLink>
                </li>
              ))}
            </ul>
          </div>

          <div className={styles.runtimeCard}>
            <div className={styles.runtimeHeader}>
              <span className="eyebrow">Host runtime</span>
              <span className="chip" data-tone={toneForStatus(backendStatus)}>
                {backendStatus}
              </span>
            </div>
            <div className="detailGrid">
              <div className="detailRow">
                <span className="detailLabel">Mode</span>
                <span className="detailValue">
                  {runtimeMode === "desktop" ? "Tauri desktop" : "Browser preview"}
                </span>
              </div>
              <div className="detailRow">
                <span className="detailLabel">Restore</span>
                <span className="detailValue">{restoreStatus}</span>
              </div>
              <div className="detailRow">
                <span className="detailLabel">Last route</span>
                <span className="detailValue">{restoreRoute}</span>
              </div>
              <div className="detailRow">
                <span className="detailLabel">Theme</span>
                <span className="detailValue">{sessionState?.theme.active_theme ?? "system"}</span>
              </div>
            </div>
            <p className={styles.runtimeNote}>{backendMessage}</p>
          </div>
        </nav>

        <main className={styles.center}>
          <div className={styles.stageBackdrop} />
          <div className={styles.stageGlow} />

          <section className={styles.stageFrame}>
            <div className={styles.stageHeader}>
              <div className={styles.stageCopy}>
                <span className="eyebrow">Shell composition</span>
                <h1 className={styles.stageTitle}>A desktop shell that feels product-ready before sync work</h1>
                <p className={styles.stageLead}>
                  The web UI now has a dedicated shell language for navigation, diagnostics, and
                  module framing while the center stage keeps Unity strictly optional.
                </p>
              </div>

              <div className="chipRow">
                <span className="chip" data-tone={toneForStatus(backendStatus)}>
                  host {backendStatus}
                </span>
                <span className="chip" data-tone={toneForStatus(unityStatus?.state)}>
                  unity {unityStatus?.state ?? "checking"}
                </span>
                <span className="chip" data-tone={toneForStatus(bridgeStatus?.state)}>
                  bridge {bridgeStatus?.state ?? "checking"}
                </span>
                <span className="chip" data-tone="accent">
                  route {routeLabel(location.pathname)}
                </span>
              </div>
            </div>

            <div className={styles.sceneGrid}>
              <section className={styles.scenePanel}>
                <div className={styles.scenePanelHeader}>
                  <div className={styles.scenePanelCopy}>
                    <span className="eyebrow">Unity boundary</span>
                    <h2 className={styles.scenePanelTitle}>Reserved center stage for room, avatar, and camera work</h2>
                    <p className={styles.scenePanelLead}>
                      Native attach is still deferred, but the shell already exposes lifecycle
                      status, launch controls, and bridge visibility from one place.
                    </p>
                  </div>

                  <div className="chipRow">
                    <span className="chip" data-tone={canLaunch ? "accent" : "sun"}>
                      {canLaunch ? "launchable" : "not launchable"}
                    </span>
                    <span className="chip" data-tone={canStop ? "danger" : "accent"}>
                      {canStop ? "stop available" : "stop idle"}
                    </span>
                  </div>
                </div>

                <div className={styles.sceneViewport}>
                  <div className={styles.sceneViewportInner}>
                    <span className={styles.sceneBadge}>3D</span>
                    <h3 className={styles.viewportTitle}>Unity runtime slot</h3>
                    <p className={styles.viewportText}>
                      Avatar performance, room framing, and animation feedback will live here after
                      S-series sync work. Until then, this stage remains a controlled runtime
                      placeholder with typed host status.
                    </p>

                    <div className="chipRow">
                      <span className="chip" data-tone={toneForStatus(unityStatus?.state)}>
                        {unityStatus?.state ?? "checking"}
                      </span>
                      <span className="chip" data-tone={toneForStatus(bridgeStatus?.state)}>
                        {bridgeStatus?.transport ?? "bridge"}
                      </span>
                    </div>
                  </div>
                </div>

                <div className={styles.sceneMetaGrid}>
                  <div className={`${styles.signalCard} surface`}>
                    <span className="eyebrow">Runtime message</span>
                    <p className={styles.signalValue}>{unityStatus?.message ?? "Reading Unity runtime..."}</p>
                    {unityStatus?.executable_path && (
                      <p className="helperText">exe {unityStatus.executable_path}</p>
                    )}
                    {unityStatus?.build_root && <p className="helperText">root {unityStatus.build_root}</p>}
                    {unityStatus?.pid && <p className="helperText">pid {unityStatus.pid}</p>}
                  </div>

                  <div className={`${styles.signalCard} surface`}>
                    <span className="eyebrow">Restore context</span>
                    <p className={styles.signalValue}>{restoreRoute}</p>
                    <p className="helperText">recent {recentRoutes.slice(0, 3).join(" / ")}</p>
                    <p className="helperText">
                      planner view {sessionState?.filters.planner_view ?? "today"}
                    </p>
                  </div>
                </div>

                <div className={styles.sceneActions}>
                  <button
                    type="button"
                    className="ghostButton"
                    onClick={refreshUnityStatus}
                    disabled={unityBusy}
                  >
                    {unityBusy ? "Reading..." : "Refresh runtime"}
                  </button>
                  <button
                    type="button"
                    className="secondaryButton"
                    onClick={startUnitySidecar}
                    disabled={!canLaunch || unityBusy}
                  >
                    Launch Unity
                  </button>
                  <button
                    type="button"
                    className="ghostButton"
                    onClick={stopUnitySidecar}
                    disabled={!canStop || unityBusy}
                  >
                    Stop Unity
                  </button>
                </div>
              </section>

              <aside className={styles.sceneSidebar}>
                <article className={`${styles.signalCard} surface`}>
                  <div className={styles.signalHeader}>
                    <span className="eyebrow">Backend host</span>
                    <span className="chip" data-tone={toneForStatus(backendStatus)}>
                      {backendStatus}
                    </span>
                  </div>
                  <p className={styles.signalValue}>{backendMessage}</p>
                  <div className="detailGrid">
                    <div className="detailRow">
                      <span className="detailLabel">Retry</span>
                      <span className="detailValue">
                        {backendEvent?.status === "error" ? "available from chrome" : "not needed"}
                      </span>
                    </div>
                    <div className="detailRow">
                      <span className="detailLabel">Window</span>
                      <span className="detailValue">
                        {sessionState?.runtime_snapshot.window_maximized ? "maximized" : "normal"}
                      </span>
                    </div>
                  </div>
                </article>

                <article className={`${styles.signalCard} surface`}>
                  <div className={styles.signalHeader}>
                    <span className="eyebrow">Unity bridge</span>
                    <span className="chip" data-tone={toneForStatus(bridgeStatus?.state)}>
                      {bridgeStatus?.state ?? "checking"}
                    </span>
                  </div>
                  <p className={styles.signalValue}>
                    {bridgeStatus?.note ?? "Reading typed React to Tauri to Unity bridge status."}
                  </p>
                  <div className="detailGrid">
                    {bridgeStatus?.listen_url && (
                      <div className="detailRow">
                        <span className="detailLabel">Listen</span>
                        <span className="detailValue">{bridgeStatus.listen_url}</span>
                      </div>
                    )}
                    {bridgeStatus?.connected_client && (
                      <div className="detailRow">
                        <span className="detailLabel">Peer</span>
                        <span className="detailValue">{bridgeStatus.connected_client}</span>
                      </div>
                    )}
                    {lastBridgeEvent && (
                      <div className="detailRow">
                        <span className="detailLabel">Last event</span>
                        <span className="detailValue">{lastBridgeEvent.type}</span>
                      </div>
                    )}
                    {bridgeStatus?.last_error && (
                      <div className="detailRow">
                        <span className="detailLabel">Last error</span>
                        <span className="detailValue">{bridgeStatus.last_error}</span>
                      </div>
                    )}
                  </div>
                </article>

                <article className={`${styles.signalCard} surface surfaceMuted`}>
                  <div className={styles.signalHeader}>
                    <span className="eyebrow">Shell system</span>
                    <span className="chip" data-tone="accent">
                      A05
                    </span>
                  </div>
                  <p className={styles.signalValue}>
                    This pass locks the shell structure, visual tokens, and page framing before
                    feature-depth work starts in A06 through A14.
                  </p>
                  <div className="chipRow">
                    <span className="chip" data-tone="accent">page template</span>
                    <span className="chip" data-tone="warm">design tokens</span>
                    <span className="chip" data-tone="sun">module framing</span>
                  </div>
                </article>
              </aside>
            </div>
          </section>
        </main>

        <aside className={styles.rightPanel}>
          <DesktopSessionContext.Provider value={{ sessionState, updateSessionState }}>
            <Outlet />
          </DesktopSessionContext.Provider>
        </aside>
      </div>
    </div>
  );
}
