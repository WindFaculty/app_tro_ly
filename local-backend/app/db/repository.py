from __future__ import annotations

import json
import sqlite3
import threading
from pathlib import Path
from typing import Any


class SQLiteRepository:
    def __init__(self, db_path: Path) -> None:
        self._db_path = db_path
        self._lock = threading.RLock()
        self._conn = sqlite3.connect(str(db_path), check_same_thread=False)
        self._conn.row_factory = sqlite3.Row
        with self._conn:
            self._conn.execute("PRAGMA foreign_keys = ON")

    @property
    def db_path(self) -> Path:
        return self._db_path

    def initialize(self) -> None:
        with self._conn:
            self._conn.executescript(
                """
                CREATE TABLE IF NOT EXISTS tasks (
                    id TEXT PRIMARY KEY,
                    title TEXT NOT NULL,
                    description TEXT,
                    status TEXT NOT NULL,
                    priority TEXT NOT NULL,
                    category TEXT,
                    scheduled_date TEXT,
                    start_at TEXT,
                    end_at TEXT,
                    due_at TEXT,
                    is_all_day INTEGER NOT NULL DEFAULT 0,
                    repeat_rule TEXT NOT NULL DEFAULT 'none',
                    repeat_config_json TEXT,
                    estimated_minutes INTEGER,
                    actual_minutes INTEGER,
                    tags_json TEXT NOT NULL DEFAULT '[]',
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    completed_at TEXT
                );

                CREATE TABLE IF NOT EXISTS task_occurrences (
                    id TEXT PRIMARY KEY,
                    task_id TEXT NOT NULL,
                    occurrence_date TEXT NOT NULL,
                    start_at TEXT,
                    end_at TEXT,
                    due_at TEXT,
                    created_at TEXT NOT NULL,
                    FOREIGN KEY(task_id) REFERENCES tasks(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS conversations (
                    id TEXT PRIMARY KEY,
                    mode TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS messages (
                    id TEXT PRIMARY KEY,
                    conversation_id TEXT NOT NULL,
                    role TEXT NOT NULL,
                    content TEXT NOT NULL,
                    emotion TEXT,
                    animation_hint TEXT,
                    metadata_json TEXT NOT NULL DEFAULT '{}',
                    created_at TEXT NOT NULL,
                    FOREIGN KEY(conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS reminders (
                    id TEXT PRIMARY KEY,
                    task_id TEXT NOT NULL,
                    remind_at TEXT NOT NULL,
                    delivered_at TEXT,
                    status TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    FOREIGN KEY(task_id) REFERENCES tasks(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS app_settings (
                    key TEXT PRIMARY KEY,
                    value_json TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS google_oauth_tokens (
                    provider TEXT PRIMARY KEY,
                    access_token TEXT,
                    refresh_token TEXT,
                    token_type TEXT,
                    scope_json TEXT NOT NULL DEFAULT '[]',
                    expires_at TEXT,
                    email_address TEXT,
                    last_sync_at TEXT,
                    last_error TEXT,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS google_oauth_states (
                    provider TEXT PRIMARY KEY,
                    state TEXT NOT NULL,
                    created_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS session_state (
                    key TEXT PRIMARY KEY,
                    value_json TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS assistant_sessions (
                    id TEXT PRIMARY KEY,
                    conversation_id TEXT,
                    mode TEXT NOT NULL,
                    voice_state TEXT NOT NULL,
                    active_route TEXT,
                    active_plan_id TEXT,
                    metadata_json TEXT NOT NULL DEFAULT '{}',
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    FOREIGN KEY(conversation_id) REFERENCES conversations(id) ON DELETE SET NULL
                );

                CREATE TABLE IF NOT EXISTS conversation_summaries (
                    conversation_id TEXT PRIMARY KEY,
                    summary_text TEXT NOT NULL,
                    turn_count INTEGER NOT NULL DEFAULT 0,
                    updated_at TEXT NOT NULL,
                    FOREIGN KEY(conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS memory_items (
                    id TEXT PRIMARY KEY,
                    category TEXT NOT NULL,
                    normalized_key TEXT NOT NULL,
                    content TEXT NOT NULL,
                    confidence REAL NOT NULL,
                    status TEXT NOT NULL,
                    metadata_json TEXT NOT NULL DEFAULT '{}',
                    source_conversation_id TEXT,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    FOREIGN KEY(source_conversation_id) REFERENCES conversations(id) ON DELETE SET NULL
                );

                CREATE UNIQUE INDEX IF NOT EXISTS idx_memory_items_key
                ON memory_items(category, normalized_key);

                CREATE TABLE IF NOT EXISTS notes (
                    id TEXT PRIMARY KEY,
                    title TEXT NOT NULL,
                    body TEXT NOT NULL DEFAULT '',
                    tags_json TEXT NOT NULL DEFAULT '[]',
                    linked_task_id TEXT,
                    linked_conversation_id TEXT,
                    pinned INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    FOREIGN KEY(linked_task_id) REFERENCES tasks(id) ON DELETE SET NULL,
                    FOREIGN KEY(linked_conversation_id) REFERENCES conversations(id) ON DELETE SET NULL
                );

                CREATE INDEX IF NOT EXISTS idx_notes_updated_at
                ON notes(updated_at DESC);

                CREATE INDEX IF NOT EXISTS idx_notes_linked_task
                ON notes(linked_task_id);

                CREATE INDEX IF NOT EXISTS idx_notes_linked_conversation
                ON notes(linked_conversation_id);

                CREATE TABLE IF NOT EXISTS email_drafts (
                    id TEXT PRIMARY KEY,
                    provider TEXT NOT NULL,
                    thread_id TEXT,
                    linked_message_id TEXT,
                    to_json TEXT NOT NULL DEFAULT '[]',
                    cc_json TEXT NOT NULL DEFAULT '[]',
                    bcc_json TEXT NOT NULL DEFAULT '[]',
                    subject TEXT NOT NULL DEFAULT '',
                    body_text TEXT NOT NULL DEFAULT '',
                    status TEXT NOT NULL DEFAULT 'draft',
                    gmail_message_id TEXT,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    sent_at TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_email_drafts_updated_at
                ON email_drafts(updated_at DESC);

                CREATE TABLE IF NOT EXISTS email_task_links (
                    email_message_id TEXT NOT NULL,
                    task_id TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    PRIMARY KEY (email_message_id, task_id),
                    FOREIGN KEY(task_id) REFERENCES tasks(id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_email_task_links_task
                ON email_task_links(task_id);

                CREATE TABLE IF NOT EXISTS browser_automation_runs (
                    id TEXT PRIMARY KEY,
                    template_id TEXT NOT NULL,
                    title TEXT NOT NULL,
                    goal TEXT NOT NULL,
                    status TEXT NOT NULL,
                    current_step_index INTEGER NOT NULL DEFAULT 0,
                    start_url TEXT,
                    inputs_json TEXT NOT NULL DEFAULT '{}',
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    completed_at TEXT,
                    cancelled_at TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_browser_automation_runs_updated_at
                ON browser_automation_runs(updated_at DESC);

                CREATE TABLE IF NOT EXISTS browser_automation_steps (
                    id TEXT PRIMARY KEY,
                    run_id TEXT NOT NULL,
                    position INTEGER NOT NULL,
                    action_type TEXT NOT NULL,
                    title TEXT NOT NULL,
                    description TEXT NOT NULL DEFAULT '',
                    status TEXT NOT NULL,
                    requires_approval INTEGER NOT NULL DEFAULT 1,
                    url TEXT,
                    payload_json TEXT NOT NULL DEFAULT '{}',
                    result_json TEXT NOT NULL DEFAULT '{}',
                    approval_note TEXT,
                    recovery_notes TEXT NOT NULL DEFAULT '',
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL,
                    completed_at TEXT,
                    FOREIGN KEY(run_id) REFERENCES browser_automation_runs(id) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS idx_browser_automation_steps_position
                ON browser_automation_steps(run_id, position);

                CREATE TABLE IF NOT EXISTS browser_automation_logs (
                    id TEXT PRIMARY KEY,
                    run_id TEXT NOT NULL,
                    step_id TEXT,
                    level TEXT NOT NULL,
                    code TEXT NOT NULL,
                    message TEXT NOT NULL,
                    payload_json TEXT NOT NULL DEFAULT '{}',
                    created_at TEXT NOT NULL,
                    FOREIGN KEY(run_id) REFERENCES browser_automation_runs(id) ON DELETE CASCADE,
                    FOREIGN KEY(step_id) REFERENCES browser_automation_steps(id) ON DELETE SET NULL
                );

                CREATE INDEX IF NOT EXISTS idx_browser_automation_logs_run_created_at
                ON browser_automation_logs(run_id, created_at ASC);

                CREATE TABLE IF NOT EXISTS route_logs (
                    id TEXT PRIMARY KEY,
                    conversation_id TEXT,
                    session_id TEXT,
                    route TEXT NOT NULL,
                    provider TEXT NOT NULL,
                    model_name TEXT,
                    latency_ms INTEGER,
                    token_usage_json TEXT NOT NULL DEFAULT '{}',
                    fallback_used INTEGER NOT NULL DEFAULT 0,
                    error_text TEXT,
                    created_at TEXT NOT NULL,
                    FOREIGN KEY(conversation_id) REFERENCES conversations(id) ON DELETE SET NULL
                );
                """
            )

    def close(self) -> None:
        self._conn.close()

    def health_check(self) -> dict[str, Any]:
        try:
            self._conn.execute("SELECT 1")
            return {"available": True, "path": str(self._db_path)}
        except sqlite3.Error as exc:
            return {"available": False, "path": str(self._db_path), "error": str(exc)}

    def create_task(self, payload: dict[str, Any]) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO tasks (
                    id, title, description, status, priority, category, scheduled_date,
                    start_at, end_at, due_at, is_all_day, repeat_rule, repeat_config_json,
                    estimated_minutes, actual_minutes, tags_json, created_at, updated_at, completed_at
                ) VALUES (
                    :id, :title, :description, :status, :priority, :category, :scheduled_date,
                    :start_at, :end_at, :due_at, :is_all_day, :repeat_rule, :repeat_config_json,
                    :estimated_minutes, :actual_minutes, :tags_json, :created_at, :updated_at, :completed_at
                )
                """,
                self._serialize_task(payload),
            )

    def update_task(self, task_id: str, updates: dict[str, Any]) -> None:
        serialized = self._serialize_task(updates, include_id=False)
        assignments = ", ".join(f"{key} = :{key}" for key in serialized)
        serialized["id"] = task_id
        with self._lock, self._conn:
            self._conn.execute(f"UPDATE tasks SET {assignments} WHERE id = :id", serialized)

    def get_task(self, task_id: str) -> dict[str, Any] | None:
        row = self._fetchone("SELECT * FROM tasks WHERE id = ?", (task_id,))
        return self._row_to_task(row) if row else None

    def list_tasks(self, where: str = "", params: tuple[Any, ...] = ()) -> list[dict[str, Any]]:
        sql = "SELECT * FROM tasks"
        if where:
            sql += f" WHERE {where}"
        sql += " ORDER BY COALESCE(start_at, due_at, scheduled_date, created_at), priority DESC, title"
        rows = self._fetchall(sql, params)
        return [self._row_to_task(row) for row in rows]

    def list_active_tasks(self) -> list[dict[str, Any]]:
        return self.list_tasks("status NOT IN ('done', 'cancelled')")

    def create_occurrence(self, payload: dict[str, Any]) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO task_occurrences (
                    id, task_id, occurrence_date, start_at, end_at, due_at, created_at
                ) VALUES (
                    :id, :task_id, :occurrence_date, :start_at, :end_at, :due_at, :created_at
                )
                """,
                payload,
            )

    def replace_occurrences(self, task_id: str, items: list[dict[str, Any]]) -> None:
        with self._lock, self._conn:
            self._conn.execute("DELETE FROM task_occurrences WHERE task_id = ?", (task_id,))
            for item in items:
                self.create_occurrence(item)

    def list_occurrences_between(self, start_date: str, end_date: str) -> list[dict[str, Any]]:
        rows = self._fetchall(
            """
            SELECT o.*, t.title, t.description, t.status, t.priority, t.category,
                   t.is_all_day, t.repeat_rule, t.repeat_config_json, t.estimated_minutes,
                   t.actual_minutes, t.tags_json, t.created_at, t.updated_at, t.completed_at
            FROM task_occurrences o
            JOIN tasks t ON t.id = o.task_id
            WHERE o.occurrence_date BETWEEN ? AND ?
              AND t.status NOT IN ('done', 'cancelled')
            ORDER BY COALESCE(o.start_at, o.due_at, o.occurrence_date), t.priority DESC, t.title
            """,
            (start_date, end_date),
        )
        items: list[dict[str, Any]] = []
        for row in rows:
            items.append(
                {
                    "id": row["task_id"],
                    "title": row["title"],
                    "description": row["description"],
                    "status": row["status"],
                    "priority": row["priority"],
                    "category": row["category"],
                    "scheduled_date": row["occurrence_date"],
                    "start_at": row["start_at"],
                    "end_at": row["end_at"],
                    "due_at": row["due_at"],
                    "is_all_day": bool(row["is_all_day"]),
                    "repeat_rule": row["repeat_rule"],
                    "estimated_minutes": row["estimated_minutes"],
                    "actual_minutes": row["actual_minutes"],
                    "tags": json.loads(row["tags_json"] or "[]"),
                    "created_at": row["created_at"],
                    "updated_at": row["updated_at"],
                    "completed_at": row["completed_at"],
                }
            )
        return items

    def create_conversation(self, payload: dict[str, Any]) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO conversations (id, mode, created_at, updated_at)
                VALUES (:id, :mode, :created_at, :updated_at)
                """,
                payload,
            )

    def touch_conversation(self, conversation_id: str, updated_at: str) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                "UPDATE conversations SET updated_at = ? WHERE id = ?",
                (updated_at, conversation_id),
            )

    def get_conversation(self, conversation_id: str) -> dict[str, Any] | None:
        row = self._fetchone("SELECT * FROM conversations WHERE id = ?", (conversation_id,))
        return dict(row) if row else None

    def list_conversations(self, limit: int = 20) -> list[dict[str, Any]]:
        rows = self._fetchall(
            """
            SELECT
                c.id,
                c.mode,
                c.created_at,
                c.updated_at,
                (
                    SELECT COUNT(*)
                    FROM messages m_count
                    WHERE m_count.conversation_id = c.id
                ) AS message_count,
                (
                    SELECT m_last.content
                    FROM messages m_last
                    WHERE m_last.conversation_id = c.id
                    ORDER BY m_last.created_at DESC, m_last.rowid DESC
                    LIMIT 1
                ) AS last_message_content,
                (
                    SELECT m_last.role
                    FROM messages m_last
                    WHERE m_last.conversation_id = c.id
                    ORDER BY m_last.created_at DESC, m_last.rowid DESC
                    LIMIT 1
                ) AS last_message_role,
                (
                    SELECT m_last.created_at
                    FROM messages m_last
                    WHERE m_last.conversation_id = c.id
                    ORDER BY m_last.created_at DESC, m_last.rowid DESC
                    LIMIT 1
                ) AS last_message_at,
                (
                    SELECT cs.summary_text
                    FROM conversation_summaries cs
                    WHERE cs.conversation_id = c.id
                ) AS summary_text
            FROM conversations c
            ORDER BY c.updated_at DESC
            LIMIT ?
            """,
            (limit,),
        )
        return [dict(row) for row in rows]

    def add_message(self, payload: dict[str, Any]) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO messages (
                    id, conversation_id, role, content, emotion, animation_hint, metadata_json, created_at
                ) VALUES (
                    :id, :conversation_id, :role, :content, :emotion, :animation_hint, :metadata_json, :created_at
                )
                """,
                payload,
            )

    def list_messages(self, conversation_id: str) -> list[dict[str, Any]]:
        rows = self._fetchall(
            "SELECT * FROM messages WHERE conversation_id = ? ORDER BY created_at ASC",
            (conversation_id,),
        )
        items = []
        for row in rows:
            data = dict(row)
            data["metadata"] = json.loads(data.pop("metadata_json") or "{}")
            items.append(data)
        return items

    def replace_reminders(self, task_id: str, reminders: list[dict[str, Any]]) -> None:
        with self._lock, self._conn:
            self._conn.execute("DELETE FROM reminders WHERE task_id = ?", (task_id,))
            for reminder in reminders:
                self._conn.execute(
                    """
                    INSERT INTO reminders (id, task_id, remind_at, delivered_at, status, created_at)
                    VALUES (:id, :task_id, :remind_at, :delivered_at, :status, :created_at)
                    """,
                    reminder,
                )

    def list_due_reminders(self, now_iso: str) -> list[dict[str, Any]]:
        rows = self._fetchall(
            """
            SELECT r.*, t.title, t.start_at, t.due_at
            FROM reminders r
            JOIN tasks t ON t.id = r.task_id
            WHERE r.status = 'pending' AND r.remind_at <= ?
            ORDER BY r.remind_at ASC
            """,
            (now_iso,),
        )
        return [dict(row) for row in rows]

    def mark_reminder_delivered(self, reminder_id: str, delivered_at: str) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                "UPDATE reminders SET delivered_at = ?, status = 'delivered' WHERE id = ?",
                (delivered_at, reminder_id),
            )

    def get_settings(self) -> dict[str, Any]:
        rows = self._fetchall("SELECT key, value_json FROM app_settings")
        return {row["key"]: json.loads(row["value_json"]) for row in rows}

    def get_setting(self, key: str) -> Any:
        row = self._fetchone("SELECT value_json FROM app_settings WHERE key = ?", (key,))
        return json.loads(row["value_json"]) if row else None

    def set_setting(self, key: str, value: Any) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO app_settings (key, value_json)
                VALUES (?, ?)
                ON CONFLICT(key) DO UPDATE SET value_json = excluded.value_json
                """,
                (key, json.dumps(value)),
            )

    def clear_settings(self) -> None:
        with self._lock, self._conn:
            self._conn.execute("DELETE FROM app_settings")

    def get_google_oauth_token(self, provider: str) -> dict[str, Any] | None:
        row = self._fetchone(
            "SELECT * FROM google_oauth_tokens WHERE provider = ?",
            (provider,),
        )
        return self._row_to_google_oauth_token(row) if row else None

    def upsert_google_oauth_token(self, payload: dict[str, Any]) -> None:
        serialized = dict(payload)
        serialized["scope_json"] = json.dumps(serialized.get("scope") or [])
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO google_oauth_tokens (
                    provider, access_token, refresh_token, token_type, scope_json, expires_at,
                    email_address, last_sync_at, last_error, created_at, updated_at
                ) VALUES (
                    :provider, :access_token, :refresh_token, :token_type, :scope_json, :expires_at,
                    :email_address, :last_sync_at, :last_error, :created_at, :updated_at
                )
                ON CONFLICT(provider) DO UPDATE SET
                    access_token = excluded.access_token,
                    refresh_token = COALESCE(excluded.refresh_token, google_oauth_tokens.refresh_token),
                    token_type = excluded.token_type,
                    scope_json = excluded.scope_json,
                    expires_at = excluded.expires_at,
                    email_address = excluded.email_address,
                    last_sync_at = excluded.last_sync_at,
                    last_error = excluded.last_error,
                    updated_at = excluded.updated_at
                """,
                serialized,
            )

    def clear_google_oauth_token(self, provider: str) -> None:
        with self._lock, self._conn:
            self._conn.execute("DELETE FROM google_oauth_tokens WHERE provider = ?", (provider,))

    def set_google_oauth_state(self, provider: str, state: str, created_at: str) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO google_oauth_states (provider, state, created_at)
                VALUES (?, ?, ?)
                ON CONFLICT(provider) DO UPDATE SET
                    state = excluded.state,
                    created_at = excluded.created_at
                """,
                (provider, state, created_at),
            )

    def get_google_oauth_state(self, provider: str) -> dict[str, Any] | None:
        row = self._fetchone("SELECT * FROM google_oauth_states WHERE provider = ?", (provider,))
        return dict(row) if row else None

    def clear_google_oauth_state(self, provider: str) -> None:
        with self._lock, self._conn:
            self._conn.execute("DELETE FROM google_oauth_states WHERE provider = ?", (provider,))

    def create_email_draft(self, payload: dict[str, Any]) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO email_drafts (
                    id, provider, thread_id, linked_message_id, to_json, cc_json, bcc_json,
                    subject, body_text, status, gmail_message_id, created_at, updated_at, sent_at
                ) VALUES (
                    :id, :provider, :thread_id, :linked_message_id, :to_json, :cc_json, :bcc_json,
                    :subject, :body_text, :status, :gmail_message_id, :created_at, :updated_at, :sent_at
                )
                """,
                self._serialize_email_draft(payload),
            )

    def update_email_draft(self, draft_id: str, updates: dict[str, Any]) -> None:
        serialized = self._serialize_email_draft(updates, include_id=False)
        assignments = ", ".join(f"{key} = :{key}" for key in serialized)
        serialized["id"] = draft_id
        with self._lock, self._conn:
            self._conn.execute(f"UPDATE email_drafts SET {assignments} WHERE id = :id", serialized)

    def get_email_draft(self, draft_id: str) -> dict[str, Any] | None:
        row = self._fetchone("SELECT * FROM email_drafts WHERE id = ?", (draft_id,))
        return self._row_to_email_draft(row) if row else None

    def list_email_drafts(self, provider: str = "gmail", limit: int = 50) -> list[dict[str, Any]]:
        rows = self._fetchall(
            """
            SELECT * FROM email_drafts
            WHERE provider = ?
            ORDER BY status = 'draft' DESC, updated_at DESC
            LIMIT ?
            """,
            (provider, limit),
        )
        return [self._row_to_email_draft(row) for row in rows]

    def add_email_task_link(self, email_message_id: str, task_id: str, created_at: str) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO email_task_links (email_message_id, task_id, created_at)
                VALUES (?, ?, ?)
                ON CONFLICT(email_message_id, task_id) DO NOTHING
                """,
                (email_message_id, task_id, created_at),
            )

    def list_email_task_links(self, email_message_ids: list[str]) -> dict[str, list[str]]:
        if not email_message_ids:
            return {}
        placeholders = ", ".join("?" for _ in email_message_ids)
        rows = self._fetchall(
            f"""
            SELECT email_message_id, task_id
            FROM email_task_links
            WHERE email_message_id IN ({placeholders})
            ORDER BY created_at ASC
            """,
            tuple(email_message_ids),
        )
        grouped: dict[str, list[str]] = {}
        for row in rows:
            grouped.setdefault(row["email_message_id"], []).append(row["task_id"])
        return grouped

    def create_browser_automation_run(self, payload: dict[str, Any]) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO browser_automation_runs (
                    id, template_id, title, goal, status, current_step_index, start_url, inputs_json,
                    created_at, updated_at, completed_at, cancelled_at
                ) VALUES (
                    :id, :template_id, :title, :goal, :status, :current_step_index, :start_url, :inputs_json,
                    :created_at, :updated_at, :completed_at, :cancelled_at
                )
                """,
                self._serialize_browser_automation_run(payload),
            )

    def update_browser_automation_run(self, run_id: str, updates: dict[str, Any]) -> None:
        serialized = self._serialize_browser_automation_run(updates, include_id=False)
        assignments = ", ".join(f"{key} = :{key}" for key in serialized)
        serialized["id"] = run_id
        with self._lock, self._conn:
            self._conn.execute(f"UPDATE browser_automation_runs SET {assignments} WHERE id = :id", serialized)

    def get_browser_automation_run(self, run_id: str) -> dict[str, Any] | None:
        row = self._fetchone("SELECT * FROM browser_automation_runs WHERE id = ?", (run_id,))
        return self._row_to_browser_automation_run(row) if row else None

    def list_browser_automation_runs(self, limit: int = 20) -> list[dict[str, Any]]:
        rows = self._fetchall(
            """
            SELECT
                r.*,
                (
                    SELECT COUNT(*)
                    FROM browser_automation_steps s
                    WHERE s.run_id = r.id
                ) AS step_count,
                (
                    SELECT s.title
                    FROM browser_automation_steps s
                    WHERE s.run_id = r.id AND s.position = r.current_step_index
                    LIMIT 1
                ) AS pending_step_title,
                (
                    SELECT l.message
                    FROM browser_automation_logs l
                    WHERE l.run_id = r.id
                    ORDER BY l.created_at DESC
                    LIMIT 1
                ) AS last_log_message
            FROM browser_automation_runs r
            ORDER BY r.updated_at DESC
            LIMIT ?
            """,
            (limit,),
        )
        items = []
        for row in rows:
            payload = self._row_to_browser_automation_run(row)
            payload["step_count"] = row["step_count"]
            payload["pending_step_title"] = row["pending_step_title"]
            payload["last_log_message"] = row["last_log_message"]
            items.append(payload)
        return items

    def create_browser_automation_steps(self, items: list[dict[str, Any]]) -> None:
        with self._lock, self._conn:
            for item in items:
                self._conn.execute(
                    """
                    INSERT INTO browser_automation_steps (
                        id, run_id, position, action_type, title, description, status, requires_approval,
                        url, payload_json, result_json, approval_note, recovery_notes, created_at,
                        updated_at, completed_at
                    ) VALUES (
                        :id, :run_id, :position, :action_type, :title, :description, :status, :requires_approval,
                        :url, :payload_json, :result_json, :approval_note, :recovery_notes, :created_at,
                        :updated_at, :completed_at
                    )
                    """,
                    self._serialize_browser_automation_step(item),
                )

    def update_browser_automation_step(self, step_id: str, updates: dict[str, Any]) -> None:
        serialized = self._serialize_browser_automation_step(updates, include_id=False)
        assignments = ", ".join(f"{key} = :{key}" for key in serialized)
        serialized["id"] = step_id
        with self._lock, self._conn:
            self._conn.execute(f"UPDATE browser_automation_steps SET {assignments} WHERE id = :id", serialized)

    def list_browser_automation_steps(self, run_id: str) -> list[dict[str, Any]]:
        rows = self._fetchall(
            "SELECT * FROM browser_automation_steps WHERE run_id = ? ORDER BY position ASC",
            (run_id,),
        )
        return [self._row_to_browser_automation_step(row) for row in rows]

    def create_browser_automation_log(self, payload: dict[str, Any]) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO browser_automation_logs (
                    id, run_id, step_id, level, code, message, payload_json, created_at
                ) VALUES (
                    :id, :run_id, :step_id, :level, :code, :message, :payload_json, :created_at
                )
                """,
                self._serialize_browser_automation_log(payload),
            )

    def list_browser_automation_logs(self, run_id: str) -> list[dict[str, Any]]:
        rows = self._fetchall(
            "SELECT * FROM browser_automation_logs WHERE run_id = ? ORDER BY created_at ASC",
            (run_id,),
        )
        return [self._row_to_browser_automation_log(row) for row in rows]

    def get_session_state(self) -> dict[str, Any]:
        rows = self._fetchall("SELECT key, value_json FROM session_state")
        return {row["key"]: json.loads(row["value_json"]) for row in rows}

    def set_session_state(self, key: str, value: Any) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO session_state (key, value_json)
                VALUES (?, ?)
                ON CONFLICT(key) DO UPDATE SET value_json = excluded.value_json
                """,
                (key, json.dumps(value)),
            )

    def upsert_assistant_session(self, payload: dict[str, Any]) -> None:
        serialized = dict(payload)
        serialized["metadata_json"] = json.dumps(serialized.get("metadata_json") or {})
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO assistant_sessions (
                    id, conversation_id, mode, voice_state, active_route, active_plan_id,
                    metadata_json, created_at, updated_at
                ) VALUES (
                    :id, :conversation_id, :mode, :voice_state, :active_route, :active_plan_id,
                    :metadata_json, :created_at, :updated_at
                )
                ON CONFLICT(id) DO UPDATE SET
                    conversation_id = excluded.conversation_id,
                    mode = excluded.mode,
                    voice_state = excluded.voice_state,
                    active_route = excluded.active_route,
                    active_plan_id = excluded.active_plan_id,
                    metadata_json = excluded.metadata_json,
                    updated_at = excluded.updated_at
                """,
                serialized,
            )

    def get_assistant_session(self, session_id: str) -> dict[str, Any] | None:
        row = self._fetchone("SELECT * FROM assistant_sessions WHERE id = ?", (session_id,))
        if row is None:
            return None
        payload = dict(row)
        payload["metadata"] = json.loads(payload.pop("metadata_json") or "{}")
        return payload

    def delete_assistant_session(self, session_id: str) -> None:
        with self._lock, self._conn:
            self._conn.execute("DELETE FROM assistant_sessions WHERE id = ?", (session_id,))

    def get_conversation_summary(self, conversation_id: str) -> dict[str, Any] | None:
        row = self._fetchone("SELECT * FROM conversation_summaries WHERE conversation_id = ?", (conversation_id,))
        return dict(row) if row else None

    def upsert_conversation_summary(
        self,
        conversation_id: str,
        summary_text: str,
        turn_count: int,
        updated_at: str,
    ) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO conversation_summaries (conversation_id, summary_text, turn_count, updated_at)
                VALUES (?, ?, ?, ?)
                ON CONFLICT(conversation_id) DO UPDATE SET
                    summary_text = excluded.summary_text,
                    turn_count = excluded.turn_count,
                    updated_at = excluded.updated_at
                """,
                (conversation_id, summary_text, turn_count, updated_at),
            )

    def list_memory_items(self, status: str = "active") -> list[dict[str, Any]]:
        rows = self._fetchall(
            "SELECT * FROM memory_items WHERE status = ? ORDER BY confidence DESC, updated_at DESC",
            (status,),
        )
        items = []
        for row in rows:
            payload = dict(row)
            payload["metadata"] = json.loads(payload.pop("metadata_json") or "{}")
            items.append(payload)
        return items

    def upsert_memory_item(self, payload: dict[str, Any]) -> None:
        serialized = dict(payload)
        serialized["metadata_json"] = json.dumps(serialized.get("metadata_json") or {})
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO memory_items (
                    id, category, normalized_key, content, confidence, status,
                    metadata_json, source_conversation_id, created_at, updated_at
                ) VALUES (
                    :id, :category, :normalized_key, :content, :confidence, :status,
                    :metadata_json, :source_conversation_id, :created_at, :updated_at
                )
                ON CONFLICT(category, normalized_key) DO UPDATE SET
                    content = excluded.content,
                    confidence = CASE
                        WHEN excluded.confidence > memory_items.confidence THEN excluded.confidence
                        ELSE memory_items.confidence
                    END,
                    status = excluded.status,
                    metadata_json = excluded.metadata_json,
                    source_conversation_id = COALESCE(excluded.source_conversation_id, memory_items.source_conversation_id),
                    updated_at = excluded.updated_at
                """,
                serialized,
            )

    def add_route_log(self, payload: dict[str, Any]) -> None:
        serialized = dict(payload)
        serialized["token_usage_json"] = json.dumps(serialized.get("token_usage_json") or {})
        serialized["fallback_used"] = 1 if serialized.get("fallback_used") else 0
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO route_logs (
                    id, conversation_id, session_id, route, provider, model_name,
                    latency_ms, token_usage_json, fallback_used, error_text, created_at
                ) VALUES (
                    :id, :conversation_id, :session_id, :route, :provider, :model_name,
                    :latency_ms, :token_usage_json, :fallback_used, :error_text, :created_at
                )
                """,
                serialized,
            )

    def create_note(self, payload: dict[str, Any]) -> None:
        with self._lock, self._conn:
            self._conn.execute(
                """
                INSERT INTO notes (
                    id, title, body, tags_json, linked_task_id, linked_conversation_id,
                    pinned, created_at, updated_at
                ) VALUES (
                    :id, :title, :body, :tags_json, :linked_task_id, :linked_conversation_id,
                    :pinned, :created_at, :updated_at
                )
                """,
                self._serialize_note(payload),
            )

    def update_note(self, note_id: str, updates: dict[str, Any]) -> None:
        serialized = self._serialize_note(updates, include_id=False)
        assignments = ", ".join(f"{key} = :{key}" for key in serialized)
        serialized["id"] = note_id
        with self._lock, self._conn:
            self._conn.execute(f"UPDATE notes SET {assignments} WHERE id = :id", serialized)

    def get_note(self, note_id: str) -> dict[str, Any] | None:
        row = self._fetchone("SELECT * FROM notes WHERE id = ?", (note_id,))
        return self._row_to_note(row) if row else None

    def list_notes(self, limit: int = 100) -> list[dict[str, Any]]:
        rows = self._fetchall(
            "SELECT * FROM notes ORDER BY pinned DESC, updated_at DESC, title ASC LIMIT ?",
            (limit,),
        )
        return [self._row_to_note(row) for row in rows]

    def list_route_logs(self, limit: int = 50) -> list[dict[str, Any]]:
        rows = self._fetchall(
            "SELECT * FROM route_logs ORDER BY created_at DESC LIMIT ?",
            (limit,),
        )
        items = []
        for row in rows:
            payload = dict(row)
            payload["token_usage"] = json.loads(payload.pop("token_usage_json") or "{}")
            payload["fallback_used"] = bool(payload["fallback_used"])
            items.append(payload)
        return items

    def _serialize_task(
        self,
        payload: dict[str, Any],
        *,
        include_id: bool = True,
    ) -> dict[str, Any]:
        data = dict(payload)
        serialized: dict[str, Any] = {}
        for key, value in data.items():
            if key == "tags":
                serialized["tags_json"] = json.dumps(value or [])
            elif key == "repeat_config_json":
                serialized[key] = json.dumps(value) if value is not None else None
            elif key == "is_all_day":
                serialized[key] = 1 if value else 0
            elif key == "id" and not include_id:
                continue
            else:
                serialized[key] = value
        return serialized

    def _serialize_note(
        self,
        payload: dict[str, Any],
        *,
        include_id: bool = True,
    ) -> dict[str, Any]:
        data = dict(payload)
        serialized: dict[str, Any] = {}
        for key, value in data.items():
            if key == "tags":
                serialized["tags_json"] = json.dumps(value or [])
            elif key == "pinned":
                serialized[key] = 1 if value else 0
            elif key == "id" and not include_id:
                continue
            else:
                serialized[key] = value
        return serialized

    def _serialize_email_draft(
        self,
        payload: dict[str, Any],
        *,
        include_id: bool = True,
    ) -> dict[str, Any]:
        data = dict(payload)
        serialized: dict[str, Any] = {}
        for key, value in data.items():
            if key in {"to", "cc", "bcc"}:
                serialized[f"{key}_json"] = json.dumps(value or [])
            elif key == "id" and not include_id:
                continue
            else:
                serialized[key] = value
        return serialized

    def _serialize_browser_automation_run(
        self,
        payload: dict[str, Any],
        *,
        include_id: bool = True,
    ) -> dict[str, Any]:
        data = dict(payload)
        serialized: dict[str, Any] = {}
        for key, value in data.items():
            if key == "inputs":
                serialized["inputs_json"] = json.dumps(value or {})
            elif key == "id" and not include_id:
                continue
            else:
                serialized[key] = value
        return serialized

    def _serialize_browser_automation_step(
        self,
        payload: dict[str, Any],
        *,
        include_id: bool = True,
    ) -> dict[str, Any]:
        data = dict(payload)
        serialized: dict[str, Any] = {}
        for key, value in data.items():
            if key == "requires_approval":
                serialized[key] = 1 if value else 0
            elif key in {"payload", "result"}:
                serialized[f"{key}_json"] = json.dumps(value or {})
            elif key == "id" and not include_id:
                continue
            else:
                serialized[key] = value
        return serialized

    def _serialize_browser_automation_log(self, payload: dict[str, Any]) -> dict[str, Any]:
        data = dict(payload)
        serialized: dict[str, Any] = {}
        for key, value in data.items():
            if key == "payload":
                serialized["payload_json"] = json.dumps(value or {})
            else:
                serialized[key] = value
        return serialized

    def _row_to_task(self, row: sqlite3.Row, *, id_key: str = "id") -> dict[str, Any]:
        return {
            "id": row[id_key],
            "title": row["title"],
            "description": row["description"],
            "status": row["status"],
            "priority": row["priority"],
            "category": row["category"],
            "scheduled_date": row["scheduled_date"],
            "start_at": row["start_at"],
            "end_at": row["end_at"],
            "due_at": row["due_at"],
            "is_all_day": bool(row["is_all_day"]),
            "repeat_rule": row["repeat_rule"],
            "estimated_minutes": row["estimated_minutes"],
            "actual_minutes": row["actual_minutes"],
            "tags": json.loads(row["tags_json"] or "[]"),
            "created_at": row["created_at"],
            "updated_at": row["updated_at"],
            "completed_at": row["completed_at"],
        }

    def _row_to_note(self, row: sqlite3.Row) -> dict[str, Any]:
        return {
            "id": row["id"],
            "title": row["title"],
            "body": row["body"],
            "tags": json.loads(row["tags_json"] or "[]"),
            "linked_task_id": row["linked_task_id"],
            "linked_conversation_id": row["linked_conversation_id"],
            "pinned": bool(row["pinned"]),
            "created_at": row["created_at"],
            "updated_at": row["updated_at"],
        }

    def _row_to_email_draft(self, row: sqlite3.Row) -> dict[str, Any]:
        return {
            "id": row["id"],
            "provider": row["provider"],
            "thread_id": row["thread_id"],
            "linked_message_id": row["linked_message_id"],
            "to": json.loads(row["to_json"] or "[]"),
            "cc": json.loads(row["cc_json"] or "[]"),
            "bcc": json.loads(row["bcc_json"] or "[]"),
            "subject": row["subject"],
            "body_text": row["body_text"],
            "status": row["status"],
            "gmail_message_id": row["gmail_message_id"],
            "created_at": row["created_at"],
            "updated_at": row["updated_at"],
            "sent_at": row["sent_at"],
        }

    def _row_to_google_oauth_token(self, row: sqlite3.Row) -> dict[str, Any]:
        return {
            "provider": row["provider"],
            "access_token": row["access_token"],
            "refresh_token": row["refresh_token"],
            "token_type": row["token_type"],
            "scope": json.loads(row["scope_json"] or "[]"),
            "expires_at": row["expires_at"],
            "email_address": row["email_address"],
            "last_sync_at": row["last_sync_at"],
            "last_error": row["last_error"],
            "created_at": row["created_at"],
            "updated_at": row["updated_at"],
        }

    def _row_to_browser_automation_run(self, row: sqlite3.Row) -> dict[str, Any]:
        return {
            "id": row["id"],
            "template_id": row["template_id"],
            "title": row["title"],
            "goal": row["goal"],
            "status": row["status"],
            "current_step_index": row["current_step_index"],
            "start_url": row["start_url"],
            "inputs": json.loads(row["inputs_json"] or "{}"),
            "created_at": row["created_at"],
            "updated_at": row["updated_at"],
            "completed_at": row["completed_at"],
            "cancelled_at": row["cancelled_at"],
        }

    def _row_to_browser_automation_step(self, row: sqlite3.Row) -> dict[str, Any]:
        return {
            "id": row["id"],
            "position": row["position"],
            "action_type": row["action_type"],
            "title": row["title"],
            "description": row["description"],
            "status": row["status"],
            "requires_approval": bool(row["requires_approval"]),
            "url": row["url"],
            "approval_note": row["approval_note"],
            "recovery_notes": row["recovery_notes"],
            "result": json.loads(row["result_json"] or "{}"),
            "updated_at": row["updated_at"],
            "completed_at": row["completed_at"],
        }

    def _row_to_browser_automation_log(self, row: sqlite3.Row) -> dict[str, Any]:
        return {
            "id": row["id"],
            "run_id": row["run_id"],
            "step_id": row["step_id"],
            "level": row["level"],
            "code": row["code"],
            "message": row["message"],
            "payload": json.loads(row["payload_json"] or "{}"),
            "created_at": row["created_at"],
        }

    def _fetchone(self, sql: str, params: tuple[Any, ...] = ()) -> sqlite3.Row | None:
        with self._lock:
            return self._conn.execute(sql, params).fetchone()

    def _fetchall(self, sql: str, params: tuple[Any, ...] = ()) -> list[sqlite3.Row]:
        with self._lock:
            return list(self._conn.execute(sql, params).fetchall())
