import type { BackendLifecycleEvent } from "@contracts";
import { type RuntimeMode } from "@/services/runtimeHost";
import { WindowChrome } from "./WindowChrome";
import styles from "./StartupScreen.module.css";

interface StartupScreenProps {
  runtimeMode: RuntimeMode;
  message: string;
  error?: string;
  isError?: boolean;
  details?: BackendLifecycleEvent | null;
  onRetry?: () => void | Promise<void>;
  retrying?: boolean;
}

export function StartupScreen({
  runtimeMode,
  message,
  error,
  isError = false,
  details = null,
  onRetry,
  retrying = false,
}: StartupScreenProps) {
  return (
    <div className={styles.screen}>
      <WindowChrome
        runtimeMode={runtimeMode}
        backendEvent={details}
        onRetryBackend={isError ? onRetry : undefined}
        retryBusy={retrying}
      />

      <div className={styles.container}>
        <div className={styles.card}>
          <div className={styles.logoMark}>A</div>
          <h1 className={styles.title}>App Tro Ly</h1>
          <p className={styles.subtitle}>Personal AI Assistant</p>

          <div className={styles.statusRow}>
            {!isError ? (
              <div className={styles.spinner} />
            ) : (
              <div className={styles.errorIcon}>!</div>
            )}
            <p className={`${styles.message} ${isError ? styles.messageError : ""}`}>
              {message}
            </p>
          </div>

          <div className={styles.metaList}>
            <p className={styles.metaRow}>
              Mode: <code>{runtimeMode}</code>
            </p>
            {details?.backend_url && (
              <p className={styles.metaRow}>
                Backend: <code>{details.backend_url}</code>
              </p>
            )}
            {details?.backend_path && (
              <p className={styles.metaRow}>
                Root: <code>{details.backend_path}</code>
              </p>
            )}
            {details?.python_path && (
              <p className={styles.metaRow}>
                Python: <code>{details.python_path}</code>
              </p>
            )}
            {details?.pid && (
              <p className={styles.metaRow}>
                PID: <code>{details.pid}</code>
              </p>
            )}
          </div>

          {error && (
            <div className={styles.errorBox}>
              <p>{error}</p>
              <p className={styles.errorHint}>
                Ensure <code>local-backend/</code> exists and Python is available.
                <br />
                See <code>docs/09-runbook.md</code>
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
