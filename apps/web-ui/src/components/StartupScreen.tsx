import styles from "./StartupScreen.module.css";

interface StartupScreenProps {
  message: string;
  error?: string;
  isError?: boolean;
}

export function StartupScreen({ message, error, isError = false }: StartupScreenProps) {
  return (
    <div className={styles.container}>
      <div className={styles.card}>
        <div className={styles.logoMark}>✦</div>
        <h1 className={styles.title}>App Trợ Lý</h1>
        <p className={styles.subtitle}>Personal AI Assistant</p>

        <div className={styles.statusRow}>
          {!isError ? (
            <div className={styles.spinner} />
          ) : (
            <div className={styles.errorIcon}>⚠</div>
          )}
          <p className={`${styles.message} ${isError ? styles.messageError : ""}`}>
            {message}
          </p>
        </div>

        {error && (
          <div className={styles.errorBox}>
            <p>{error}</p>
            <p className={styles.errorHint}>
              Hãy đảm bảo <code>local-backend/</code> đã được cài đặt và Python available.
              <br />Xem: <code>docs/09-runbook.md</code>
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
