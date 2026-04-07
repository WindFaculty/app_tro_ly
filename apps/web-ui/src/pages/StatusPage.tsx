import { useEffect, useState } from "react";
import { PageTemplate } from "@/components/PageTemplate";
import styles from "./StatusPage.module.css";
import type { HealthResponse } from "@/contracts/backend";
import { checkHealth, getBackendBaseUrlForUi } from "@/services/backendClient";

export function StatusPage() {
  const [health, setHealth] = useState<HealthResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>("");
  const [backendUrl, setBackendUrl] = useState<string>("");

  const refresh = async () => {
    setLoading(true);
    setError("");
    try {
      const [data, url] = await Promise.all([checkHealth(), getBackendBaseUrlForUi()]);
      setHealth(data);
      setBackendUrl(url);
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { refresh(); }, []);

  const statusColor = (s: string) => {
    if (s === "ready") return styles.statusReady;
    if (s === "partial") return styles.statusPartial;
    return styles.statusError;
  };

  return (
    <PageTemplate title="System Status" icon="🩺">
      <div className={styles.container}>
        <button className={styles.refreshBtn} onClick={refresh}>
          ↻ Refresh
        </button>

        {loading && <p className={styles.loading}>Đang kiểm tra...</p>}

        {!loading && error && (
          <div className={styles.errorBox}>
            <p>⚠ Không thể kết nối backend</p>
            <p className={styles.errorDetail}>{error}</p>
          </div>
        )}

        {!loading && health && (
          <div className={styles.healthCard}>
            <div className={styles.row}>
              <span className={styles.label}>URL</span>
              <span className={styles.value}>{backendUrl}</span>
            </div>
            <div className={styles.row}>
              <span className={styles.label}>Status</span>
              <span className={`${styles.badge} ${statusColor(health.status)}`}>
                {health.status.toUpperCase()}
              </span>
            </div>
            {health.service && (
              <div className={styles.row}>
                <span className={styles.label}>Service</span>
                <span className={styles.value}>{health.service}</span>
              </div>
            )}
            {health.version && (
              <div className={styles.row}>
                <span className={styles.label}>Version</span>
                <span className={styles.value}>{health.version}</span>
              </div>
            )}
            {health.degraded_features && health.degraded_features.length > 0 && (
              <div className={styles.row}>
                <span className={styles.label}>Degraded</span>
                <span className={styles.degraded}>
                  {health.degraded_features.join(", ")}
                </span>
              </div>
            )}
            {health.recovery_actions.length > 0 && (
              <div className={styles.row}>
                <span className={styles.label}>Recovery</span>
                <span className={styles.degraded}>
                  {health.recovery_actions.join(", ")}
                </span>
              </div>
            )}
          </div>
        )}

        <div className={styles.legend}>
          <div className={styles.legendItem}>
            <span className={`${styles.dot} ${styles.statusReady}`} />
            <span>ready — fully operational</span>
          </div>
          <div className={styles.legendItem}>
            <span className={`${styles.dot} ${styles.statusPartial}`} />
            <span>partial — some features degraded</span>
          </div>
          <div className={styles.legendItem}>
            <span className={`${styles.dot} ${styles.statusError}`} />
            <span>error — backend unavailable</span>
          </div>
        </div>
      </div>
    </PageTemplate>
  );
}
