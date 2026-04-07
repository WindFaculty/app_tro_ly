import type { ReactNode } from "react";
import styles from "./PageTemplate.module.css";

interface PageHighlight {
  label: string;
  value: string;
  detail?: string;
}

interface PageTemplateProps {
  title: string;
  icon: string;
  eyebrow?: string;
  description?: string;
  highlights?: PageHighlight[];
  actions?: ReactNode;
  children?: ReactNode;
}

export function PageTemplate({
  title,
  icon,
  eyebrow = "Desktop module",
  description = "",
  highlights = [],
  actions,
  children,
}: PageTemplateProps) {
  return (
    <section className={`${styles.page} pageTransition`}>
      <header className={`${styles.hero} surface surfaceHero`}>
        <div className={styles.heroCopy}>
          <div className={styles.titleRow}>
            <span className={styles.iconBadge}>{icon}</span>
            <div className={styles.headingBlock}>
              <span className="eyebrow">{eyebrow}</span>
              <h2 className={styles.title}>{title}</h2>
            </div>
          </div>
          {description && <p className={styles.description}>{description}</p>}
        </div>

        {actions && <div className={styles.actions}>{actions}</div>}
      </header>

      {highlights.length > 0 && (
        <div className={styles.highlightGrid}>
          {highlights.map((item) => (
            <article key={item.label} className={`${styles.highlightCard} surface`}>
              <span className={styles.highlightLabel}>{item.label}</span>
              <strong className={styles.highlightValue}>{item.value}</strong>
              {item.detail && <p className={styles.highlightDetail}>{item.detail}</p>}
            </article>
          ))}
        </div>
      )}

      <div className={styles.content}>
        {children ?? (
          <div className="emptyState">
            <p className="emptyStateTitle">Module shell ready</p>
            <p className="emptyStateText">
              This page now uses the shared desktop design system and is ready for feature depth in
              later phases.
            </p>
          </div>
        )}
      </div>
    </section>
  );
}
