import { useEffect, useState } from "react";
import type { BackendLifecycleEvent } from "@contracts";
import styles from "./WindowChrome.module.css";
import {
  closeDesktopWindow,
  getShellRuntimeState,
  minimizeDesktopWindow,
  toggleDesktopWindowMaximize,
  type RuntimeMode,
  type ShellRuntimeState,
} from "@/services/runtimeHost";

interface WindowChromeProps {
  runtimeMode: RuntimeMode;
  backendEvent?: BackendLifecycleEvent | null;
  onRetryBackend?: () => void | Promise<void>;
  retryBusy?: boolean;
  compact?: boolean;
}

export function WindowChrome({
  runtimeMode,
  backendEvent,
  onRetryBackend,
  retryBusy = false,
  compact = false,
}: WindowChromeProps) {
  const [shellState, setShellState] = useState<ShellRuntimeState | null>(null);
  const [windowBusy, setWindowBusy] = useState(false);

  useEffect(() => {
    let mounted = true;

    getShellRuntimeState()
      .then((state) => {
        if (mounted) {
          setShellState(state);
        }
      })
      .catch(() => undefined);

    return () => {
      mounted = false;
    };
  }, [runtimeMode]);

  const status = backendEvent?.status ?? shellState?.backend_status ?? runtimeMode;
  const message =
    backendEvent?.message ??
    shellState?.backend_message ??
    (runtimeMode === "desktop" ? "Desktop shell is active." : "Browser preview mode.");
  const isDesktop = runtimeMode === "desktop";
  const showRetry = Boolean(onRetryBackend) && status !== "ready";
  const statusClassName =
    status === "ready"
      ? styles.statusReady
      : status === "error"
        ? styles.statusError
        : styles.statusLoading;

  const handleMinimize = async () => {
    setWindowBusy(true);
    try {
      await minimizeDesktopWindow();
    } finally {
      setWindowBusy(false);
    }
  };

  const handleToggleMaximize = async () => {
    setWindowBusy(true);
    try {
      setShellState(await toggleDesktopWindowMaximize());
    } finally {
      setWindowBusy(false);
    }
  };

  const handleClose = async () => {
    setWindowBusy(true);
    try {
      await closeDesktopWindow();
    } finally {
      setWindowBusy(false);
    }
  };

  return (
    <header className={`${styles.chrome} ${compact ? styles.compact : ""}`} data-tauri-drag-region>
      <div className={styles.brand} data-tauri-drag-region>
        <span className={styles.logo}>A</span>
        <div className={styles.brandCopy} data-tauri-drag-region>
          <span className={styles.title}>App Tro Ly</span>
          <span className={styles.subtitle}>
            {isDesktop ? "Tauri desktop shell" : "Browser preview"}
          </span>
        </div>
      </div>

      <div className={styles.statusBlock} data-tauri-drag-region>
        <span className={`${styles.statusBadge} ${statusClassName}`}>{status}</span>
        <span className={styles.statusMessage}>{message}</span>
      </div>

      <div className={styles.actionRow}>
        {showRetry && (
          <button
            type="button"
            className={styles.retryButton}
            onClick={() => void onRetryBackend?.()}
            disabled={retryBusy}
          >
            {retryBusy ? "Retrying..." : "Retry backend"}
          </button>
        )}

        {isDesktop && (
          <div className={styles.windowControls}>
            <button
              type="button"
              className={styles.windowButton}
              onClick={() => void handleMinimize()}
              disabled={windowBusy}
              aria-label="Minimize window"
            >
              _
            </button>
            <button
              type="button"
              className={styles.windowButton}
              onClick={() => void handleToggleMaximize()}
              disabled={windowBusy}
              aria-label="Toggle maximize window"
            >
              {shellState?.window_maximized ? "Restore" : "Maximize"}
            </button>
            <button
              type="button"
              className={`${styles.windowButton} ${styles.closeButton}`}
              onClick={() => void handleClose()}
              disabled={windowBusy}
              aria-label="Close window"
            >
              Close
            </button>
          </div>
        )}
      </div>
    </header>
  );
}
