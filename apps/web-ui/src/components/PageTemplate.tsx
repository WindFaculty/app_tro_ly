import type { ReactNode } from "react";
import styles from "./PageTemplate.module.css";

interface PageTemplateProps {
  title: string;
  icon: string;
  children?: ReactNode;
}

export function PageTemplate({ title, icon, children }: PageTemplateProps) {
  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <span className={styles.icon}>{icon}</span>
        <h2 className={styles.title}>{title}</h2>
      </div>
      <div className={styles.content}>
        {children ?? (
          <div className={styles.placeholder}>
            <p>Phase 3 — React UI skeleton đã được dựng</p>
            <p className={styles.placeholderNote}>
              Nội dung feature sẽ tiếp tục được lấp đầy ở các phase sau
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
