import { useMemo, useState } from "react";
import { PageTemplate } from "@/components/PageTemplate";
import type {
  BrowserAutomationRunDetail,
  BrowserAutomationStepRecord,
  BrowserAutomationTemplateRecord,
} from "@/contracts/backend";
import { useBrowserAutomationWorkspace } from "@/features/browser-automation/useBrowserAutomationWorkspace";
import styles from "./AutomationPage.module.css";

function formatTimestamp(value?: string | null): string {
  if (!value) {
    return "No timestamp";
  }

  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(parsed);
}

function toneForRunStatus(status: string): "accent" | "sun" | "success" | "danger" | "warm" {
  if (status === "completed") {
    return "success";
  }
  if (status === "blocked" || status === "failed" || status === "cancelled") {
    return "danger";
  }
  if (status === "running") {
    return "warm";
  }
  if (status === "awaiting_approval") {
    return "sun";
  }
  return "accent";
}

function toneForStepStatus(status: string): "accent" | "sun" | "success" | "danger" | "warm" {
  if (status === "completed") {
    return "success";
  }
  if (status === "failed" || status === "rejected" || status === "cancelled") {
    return "danger";
  }
  if (status === "running") {
    return "warm";
  }
  if (status === "pending_approval") {
    return "sun";
  }
  return "accent";
}

function templateHint(template: BrowserAutomationTemplateRecord | null): string {
  if (!template) {
    return "Choose a bounded template before creating a run.";
  }
  return template.description;
}

function currentStep(run: BrowserAutomationRunDetail | null): BrowserAutomationStepRecord | null {
  if (!run) {
    return null;
  }
  return (
    run.steps.find((step) => step.status === "pending_approval") ??
    run.steps.find((step) => step.status === "running") ??
    null
  );
}

function createDisabled(templateId: string, title: string, goal: string, startUrl: string, query: string): boolean {
  if (!title.trim() || !goal.trim()) {
    return true;
  }
  if (templateId === "search_query_review") {
    return !query.trim();
  }
  return !startUrl.trim();
}

export function AutomationPage() {
  const workspace = useBrowserAutomationWorkspace();
  const [decisionNote, setDecisionNote] = useState("");
  const selectedTemplate = useMemo(
    () =>
      workspace.state.templates.find((item) => item.template_id === workspace.draft.templateId) ?? null,
    [workspace.draft.templateId, workspace.state.templates],
  );
  const selectedRun = workspace.state.selectedRun;
  const pendingStep = currentStep(selectedRun);
  const logItems = selectedRun?.logs.slice().reverse() ?? [];
  const createIsDisabled = createDisabled(
    workspace.draft.templateId,
    workspace.draft.title,
    workspace.draft.goal,
    workspace.draft.startUrl,
    workspace.draft.query,
  );

  return (
    <PageTemplate
      title="Browser Automation"
      icon="BA"
      eyebrow="Approval-first lane"
      description="A11 adds a bounded browser automation workspace with deterministic templates, per-step approval, audit logs, cancel flow, and recovery notes without hiding the action plan from the user."
      highlights={[
        {
          label: "Templates",
          value: String(workspace.state.templates.length),
          detail: "Bounded flows only, no opaque free-form automation",
        },
        {
          label: "Runs",
          value: String(workspace.state.runs.length),
          detail: selectedRun?.status ? `Selected run is ${selectedRun.status}` : "No run selected yet",
        },
        {
          label: "Pending step",
          value: pendingStep ? String(pendingStep.position + 1) : "none",
          detail: pendingStep?.title ?? "Approve a run to advance one step at a time",
        },
        {
          label: "Audit log",
          value: String(selectedRun?.logs.length ?? 0),
          detail: "Every approval, completion, rejection, and cancel event is persisted",
        },
      ]}
      actions={
        <>
          <button
            type="button"
            className="ghostButton"
            onClick={() => void workspace.refresh()}
            disabled={workspace.state.loading || workspace.state.mutating}
          >
            Refresh
          </button>
          <button
            type="button"
            className="primaryButton"
            onClick={() => void workspace.createRun()}
            disabled={workspace.state.mutating || createIsDisabled}
          >
            Create run
          </button>
        </>
      }
    >
      <div className="appStack">
        <article className={`${styles.hero} surface surfaceHero`}>
          <div className="surfaceHeader">
            <div className="surfaceHeaderBlock">
              <span className="eyebrow">Current implementation</span>
              <h3 className="surfaceTitle">Backend-owned, auditable browser actions with explicit checkpoints</h3>
              <p className="surfaceIntro">
                The desktop shell now models browser work as deterministic steps. Each step stops
                for approval, records a result payload, and keeps recovery notes visible when the
                run blocks or gets cancelled.
              </p>
            </div>
            <div className="chipRow">
              <span className="chip" data-tone={toneForRunStatus(selectedRun?.status ?? "awaiting_approval")}>
                {selectedRun?.status ?? "awaiting_approval"}
              </span>
              <span className="chip" data-tone="accent">
                {selectedTemplate?.template_id ?? "template pending"}
              </span>
            </div>
          </div>

          <div className={styles.heroGrid}>
            <div className={styles.heroBlock}>
              <span className="formLabel">Bounded template</span>
              <p className="helperText">{templateHint(selectedTemplate)}</p>
            </div>
            <div className={styles.heroBlock}>
              <span className="formLabel">Safety rule</span>
              <p className="helperText">
                Runs only advance one approved step at a time, and the operator can reject or cancel
                before any hidden follow-up action occurs.
              </p>
            </div>
          </div>
        </article>

        {workspace.state.loading && (
          <article className="surface">
            <span className="eyebrow">Loading</span>
            <p className="helperText">Refreshing automation templates, run history, and the selected audit trail.</p>
          </article>
        )}

        {!!workspace.state.error && (
          <article className="surface">
            <span className="eyebrow">Automation issue</span>
            <p className="errorText">{workspace.state.error}</p>
          </article>
        )}

        {workspace.state.mutationMessage && (
          <article className="surface surfaceMuted">
            <span className="eyebrow">Recent action</span>
            <p className="helperText">{workspace.state.mutationMessage}</p>
          </article>
        )}

        <div className={styles.workspaceGrid}>
          <div className={styles.leftColumn}>
            <article className="surface">
              <div className="surfaceHeader">
                <div className="surfaceHeaderBlock">
                  <span className="eyebrow">Run builder</span>
                  <h3 className="surfaceTitle">Create a bounded automation request</h3>
                  <p className="surfaceIntro">
                    Build the run from a predefined template, then review each step before the
                    backend executes it.
                  </p>
                </div>
              </div>

              <div className={styles.builderForm}>
                <label className={styles.fieldBlock}>
                  <span className="formLabel">Template</span>
                  <select
                    className="textInput"
                    value={workspace.draft.templateId}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({
                        ...current,
                        templateId: event.target.value,
                      }))
                    }
                  >
                    {workspace.state.templates.map((template) => (
                      <option key={template.template_id} value={template.template_id}>
                        {template.title}
                      </option>
                    ))}
                  </select>
                </label>

                <label className={styles.fieldBlock}>
                  <span className="formLabel">Run title</span>
                  <input
                    className="textInput"
                    value={workspace.draft.title}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({
                        ...current,
                        title: event.target.value,
                      }))
                    }
                    placeholder="Review billing portal, verify docs page, inspect search results..."
                  />
                </label>

                <label className={styles.fieldBlock}>
                  <span className="formLabel">Goal</span>
                  <textarea
                    className="textArea"
                    value={workspace.draft.goal}
                    onChange={(event) =>
                      workspace.setDraft((current) => ({
                        ...current,
                        goal: event.target.value,
                      }))
                    }
                    placeholder="Describe what this bounded run should achieve and why each step still needs human approval."
                  />
                </label>

                {workspace.draft.templateId === "search_query_review" ? (
                  <div className={styles.inlineFields}>
                    <label className={styles.fieldBlock}>
                      <span className="formLabel">Query</span>
                      <input
                        className="textInput"
                        value={workspace.draft.query}
                        onChange={(event) =>
                          workspace.setDraft((current) => ({
                            ...current,
                            query: event.target.value,
                          }))
                        }
                        placeholder="tauri desktop shell"
                      />
                    </label>
                    <label className={styles.fieldBlock}>
                      <span className="formLabel">Provider</span>
                      <select
                        className="textInput"
                        value={workspace.draft.provider}
                        onChange={(event) =>
                          workspace.setDraft((current) => ({
                            ...current,
                            provider: event.target.value,
                          }))
                        }
                      >
                        <option value="duckduckgo">DuckDuckGo</option>
                        <option value="bing">Bing</option>
                        <option value="google">Google</option>
                      </select>
                    </label>
                  </div>
                ) : (
                  <label className={styles.fieldBlock}>
                    <span className="formLabel">Start URL</span>
                    <input
                      className="textInput"
                      value={workspace.draft.startUrl}
                      onChange={(event) =>
                        workspace.setDraft((current) => ({
                          ...current,
                          startUrl: event.target.value,
                        }))
                      }
                      placeholder="https://example.com/"
                    />
                  </label>
                )}
              </div>
            </article>

            <article className="surface">
              <div className="surfaceHeader">
                <div className="surfaceHeaderBlock">
                  <span className="eyebrow">Run history</span>
                  <h3 className="surfaceTitle">Recent approval-first runs</h3>
                </div>
                <span className="chip" data-tone="accent">
                  {workspace.state.runs.length}
                </span>
              </div>

              {workspace.state.runs.length ? (
                <div className={styles.runList}>
                  {workspace.state.runs.map((run) => (
                    <button
                      key={run.id}
                      type="button"
                      className={`${styles.runCard} ${
                        workspace.state.selectedRunId === run.id ? styles.runCardActive : ""
                      }`}
                      onClick={() => void workspace.selectRun(run.id)}
                    >
                      <div className={styles.runCardHeader}>
                        <div>
                          <p className="listTitle">{run.title}</p>
                          <p className="listSubtitle">
                            {run.template_id} | updated {formatTimestamp(run.updated_at)}
                          </p>
                        </div>
                        <span className="chip" data-tone={toneForRunStatus(run.status)}>
                          {run.status}
                        </span>
                      </div>
                      <p className={styles.runGoal}>{run.goal}</p>
                      <div className="chipRow">
                        <span className="chip" data-tone="accent">
                          steps {run.step_count}
                        </span>
                        {run.pending_step_title && (
                          <span className="chip" data-tone="sun">
                            next {run.pending_step_title}
                          </span>
                        )}
                      </div>
                    </button>
                  ))}
                </div>
              ) : (
                <div className="emptyState">
                  <p className="emptyStateTitle">No automation runs yet</p>
                  <p className="emptyStateText">
                    Create the first bounded run to start reviewing and approving browser steps.
                  </p>
                </div>
              )}
            </article>
          </div>

          <article className="surface">
            <div className="surfaceHeader">
              <div className="surfaceHeaderBlock">
                <span className="eyebrow">Approval flow</span>
                <h3 className="surfaceTitle">Inspect the selected run step by step</h3>
                <p className="surfaceIntro">
                  Each step stays visible with its action type, result payload, and recovery notes.
                </p>
              </div>
              {selectedRun && (
                <span className="chip" data-tone={toneForRunStatus(selectedRun.status)}>
                  {selectedRun.status}
                </span>
              )}
            </div>

            {selectedRun ? (
              <div className={styles.detailStack}>
                <div className={styles.summaryCard}>
                  <div className="detailGrid">
                    <div className="detailRow">
                      <span className="detailLabel">Goal</span>
                      <span className="detailValue">{selectedRun.goal}</span>
                    </div>
                    <div className="detailRow">
                      <span className="detailLabel">Start URL</span>
                      <span className="detailValue">{selectedRun.start_url ?? "n/a"}</span>
                    </div>
                    <div className="detailRow">
                      <span className="detailLabel">Created</span>
                      <span className="detailValue">{formatTimestamp(selectedRun.created_at)}</span>
                    </div>
                    <div className="detailRow">
                      <span className="detailLabel">Last log</span>
                      <span className="detailValue">{selectedRun.last_log_message ?? "No audit entries yet"}</span>
                    </div>
                  </div>
                </div>

                {pendingStep && (
                  <div className={styles.approvalPanel}>
                    <div className="surfaceHeader">
                      <div className="surfaceHeaderBlock">
                        <span className="eyebrow">Current checkpoint</span>
                        <h3 className="surfaceTitle">{pendingStep.title}</h3>
                      </div>
                      <span className="chip" data-tone={toneForStepStatus(pendingStep.status)}>
                        step {pendingStep.position + 1}
                      </span>
                    </div>
                    <p className="helperText">{pendingStep.description}</p>
                    {pendingStep.url && (
                      <p className="helperText">
                        Target URL: <code>{pendingStep.url}</code>
                      </p>
                    )}
                    <textarea
                      className="textArea"
                      value={decisionNote}
                      onChange={(event) => setDecisionNote(event.target.value)}
                      placeholder="Add an approval note, rejection reason, or cancellation context for the audit trail."
                    />
                    <div className="actionRow">
                      <div className="chipRow">
                        <span className="chip" data-tone="sun">
                          recovery visible
                        </span>
                        <span className="chip" data-tone="accent">
                          audit persisted
                        </span>
                      </div>
                      <div className="chipRow">
                        <button
                          type="button"
                          className="ghostButton"
                          onClick={() => void workspace.cancelSelectedRun(decisionNote)}
                          disabled={workspace.state.mutating}
                        >
                          Cancel run
                        </button>
                        <button
                          type="button"
                          className="secondaryButton"
                          onClick={() => void workspace.rejectSelectedRun(decisionNote)}
                          disabled={workspace.state.mutating || !decisionNote.trim()}
                        >
                          Reject step
                        </button>
                        <button
                          type="button"
                          className="primaryButton"
                          onClick={() => void workspace.approveSelectedRun(decisionNote)}
                          disabled={workspace.state.mutating}
                        >
                          Approve next step
                        </button>
                      </div>
                    </div>
                  </div>
                )}

                <div className={styles.stepList}>
                  {selectedRun.steps.map((step) => (
                    <article
                      key={step.id}
                      className={`${styles.stepCard} ${
                        pendingStep?.id === step.id ? styles.stepCardActive : ""
                      }`}
                    >
                      <div className={styles.stepHeader}>
                        <div>
                          <p className="listTitle">
                            {step.position + 1}. {step.title}
                          </p>
                          <p className="listSubtitle">
                            {step.action_type} | updated {formatTimestamp(step.updated_at)}
                          </p>
                        </div>
                        <span className="chip" data-tone={toneForStepStatus(step.status)}>
                          {step.status}
                        </span>
                      </div>
                      <p className="helperText">{step.description}</p>
                      {step.url && (
                        <p className="helperText">
                          URL: <code>{step.url}</code>
                        </p>
                      )}
                      {Object.keys(step.result).length > 0 && (
                        <div className={styles.resultBlock}>
                          <span className="formLabel">Result</span>
                          <pre className={styles.resultText}>
                            {JSON.stringify(step.result, null, 2)}
                          </pre>
                        </div>
                      )}
                      <div className={styles.recoveryBlock}>
                        <span className="formLabel">Recovery notes</span>
                        <p className="helperText">{step.recovery_notes || "No recovery notes recorded."}</p>
                        {step.approval_note && (
                          <p className="helperText">
                            Operator note: <strong>{step.approval_note}</strong>
                          </p>
                        )}
                      </div>
                    </article>
                  ))}
                </div>
              </div>
            ) : (
              <div className="emptyState">
                <p className="emptyStateTitle">Select a run to inspect it</p>
                <p className="emptyStateText">
                  The approval lane will show step status, result payloads, and recovery notes for
                  the selected run.
                </p>
              </div>
            )}
          </article>

          <div className={styles.sideColumn}>
            <article className="surface">
              <div className="surfaceHeader">
                <div className="surfaceHeaderBlock">
                  <span className="eyebrow">Audit trail</span>
                  <h3 className="surfaceTitle">Persisted log entries</h3>
                </div>
                <span className="chip" data-tone="accent">
                  {logItems.length}
                </span>
              </div>

              {logItems.length ? (
                <div className="listStack">
                  {logItems.map((entry) => (
                    <div key={entry.id} className="listRow">
                      <div>
                        <p className="listTitle">{entry.message}</p>
                        <p className="listSubtitle">
                          {entry.code} | {entry.level} | {formatTimestamp(entry.created_at)}
                        </p>
                      </div>
                      <span className="chip" data-tone={entry.level === "error" ? "danger" : entry.level === "warning" ? "sun" : "accent"}>
                        {entry.level}
                      </span>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="helperText">Audit entries will appear here once a run is created.</p>
              )}
            </article>

            <article className="surface surfaceMuted">
              <div className="surfaceHeader">
                <div className="surfaceHeaderBlock">
                  <span className="eyebrow">Recovery posture</span>
                  <h3 className="surfaceTitle">Why this stays safe</h3>
                </div>
              </div>
              <div className="detailGrid">
                <div className="detailRow">
                  <span className="detailLabel">Templates</span>
                  <span className="detailValue">Only bounded templates can create runs.</span>
                </div>
                <div className="detailRow">
                  <span className="detailLabel">Approval</span>
                  <span className="detailValue">The backend executes only the next approved step.</span>
                </div>
                <div className="detailRow">
                  <span className="detailLabel">Audit</span>
                  <span className="detailValue">Each approval, rejection, cancel, and result payload is stored in SQLite.</span>
                </div>
                <div className="detailRow">
                  <span className="detailLabel">Recovery</span>
                  <span className="detailValue">
                    Step-level recovery notes stay visible even when a run blocks before completion.
                  </span>
                </div>
              </div>
            </article>
          </div>
        </div>
      </div>
    </PageTemplate>
  );
}
