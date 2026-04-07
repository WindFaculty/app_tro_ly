import { useEffect, useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import type { UnityBridgeEvent, UnityBridgeStatus } from "@contracts";
import styles from "./ShellLayout.module.css";
import {
  getUnityRuntimeStatus,
  launchUnityRuntime,
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

const NAV_ITEMS = [
  { to: "/", label: "Dashboard", icon: "□" },
  { to: "/chat", label: "Chat", icon: "💬" },
  { to: "/planner", label: "Planner", icon: "🗓" },
  { to: "/wardrobe", label: "Wardrobe", icon: "🧥" },
  { to: "/settings", label: "Settings", icon: "⚙" },
  { to: "/status", label: "Status", icon: "🩺" },
];

interface ShellLayoutProps {
  runtimeMode: RuntimeMode;
}

export function ShellLayout({ runtimeMode }: ShellLayoutProps) {
  const location = useLocation();
  const [unityStatus, setUnityStatus] = useState<UnityRuntimeStatus | null>(null);
  const [bridgeStatus, setBridgeStatus] = useState<UnityBridgeStatus | null>(null);
  const [lastBridgeEvent, setLastBridgeEvent] = useState<UnityBridgeEvent | null>(null);
  const [unityBusy, setUnityBusy] = useState(false);

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

  return (
    <div className={styles.shell}>
      <nav className={styles.sideNav}>
        <div className={styles.logo}>
          <span className={styles.logoIcon}>✦</span>
          <span className={styles.logoText}>Tro Ly</span>
        </div>
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
                <span className={styles.navLabel}>{item.label}</span>
              </NavLink>
            </li>
          ))}
        </ul>

        <div className={styles.runtimeBlock}>
          <span className={styles.runtimeLabel}>Host</span>
          <span className={styles.runtimeValue}>
            {runtimeMode === "desktop" ? "Tauri" : "Browser"}
          </span>
          <span className={styles.runtimeHint}>
            {runtimeMode === "desktop" ? "Backend auto-start" : "Backend run thu cong"}
          </span>
        </div>
      </nav>

      <main className={styles.center}>
        <div className={styles.unityPlaceholder}>
          <div className={styles.unityLabel}>
            <span className={styles.unityIcon}>🎮</span>
            <p>Unity 3D Runtime</p>
            <p className={styles.unitySubtext}>Avatar · Room · Animation</p>
            <p className={styles.unityNote}>
              Phase 6 da co typed bridge scaffold truoc khi attach native window that.
            </p>
          </div>

          <div className={styles.unityStatusCard}>
            <div className={styles.unityStatusHeader}>
              <span className={styles.unityStatusLabel}>Unity Host Status</span>
              <span className={styles.unityStatusBadge}>
                {unityStatus?.state ?? "checking"}
              </span>
            </div>

            <p className={styles.unityStatusMessage}>
              {unityStatus?.message ?? "Dang doc trang thai Unity runtime..."}
            </p>

            {unityStatus?.executable_path && (
              <p className={styles.unityMeta}>
                exe: <code>{unityStatus.executable_path}</code>
              </p>
            )}
            {unityStatus?.build_root && (
              <p className={styles.unityMeta}>
                root: <code>{unityStatus.build_root}</code>
              </p>
            )}
            {unityStatus?.pid && (
              <p className={styles.unityMeta}>
                pid: <code>{unityStatus.pid}</code>
              </p>
            )}

            <div className={styles.unityActions}>
              <button
                type="button"
                className={styles.unityActionButton}
                onClick={refreshUnityStatus}
                disabled={unityBusy}
              >
                {unityBusy ? "Dang doc..." : "Refresh"}
              </button>
              <button
                type="button"
                className={styles.unityActionButton}
                onClick={startUnitySidecar}
                disabled={!canLaunch || unityBusy}
              >
                Launch
              </button>
              <button
                type="button"
                className={styles.unityActionButton}
                onClick={stopUnitySidecar}
                disabled={!canStop || unityBusy}
              >
                Stop
              </button>
            </div>

            <p className={styles.unityFootnote}>
              Native attach, resize sync, focus sync, va embed that van can runtime validation.
            </p>
          </div>

          <div className={styles.unityStatusCard}>
            <div className={styles.unityStatusHeader}>
              <span className={styles.unityStatusLabel}>Unity Bridge</span>
              <span className={styles.unityStatusBadge}>
                {bridgeStatus?.state ?? "checking"}
              </span>
            </div>

            <p className={styles.unityStatusMessage}>
              {bridgeStatus?.note ?? "Dang doc trang thai bridge React ↔ Tauri ↔ Unity..."}
            </p>

            {bridgeStatus?.listen_url && (
              <p className={styles.unityMeta}>
                ws: <code>{bridgeStatus.listen_url}</code>
              </p>
            )}
            {bridgeStatus?.connected_client && (
              <p className={styles.unityMeta}>
                peer: <code>{bridgeStatus.connected_client}</code>
              </p>
            )}
            {bridgeStatus?.last_command_type && (
              <p className={styles.unityMeta}>
                cmd: <code>{bridgeStatus.last_command_type}</code>
              </p>
            )}
            {bridgeStatus?.last_event_type && (
              <p className={styles.unityMeta}>
                evt: <code>{bridgeStatus.last_event_type}</code>
              </p>
            )}
            {bridgeStatus?.last_error && (
              <p className={styles.unityMeta}>
                err: <code>{bridgeStatus.last_error}</code>
              </p>
            )}
            {lastBridgeEvent && (
              <p className={styles.unityFootnote}>
                Last event: <code>{lastBridgeEvent.type}</code>
              </p>
            )}
          </div>
        </div>
      </main>

      <aside className={styles.rightPanel}>
        <Outlet />
      </aside>
    </div>
  );
}
