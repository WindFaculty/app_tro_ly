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

                CREATE TABLE IF NOT EXISTS session_state (
                    key TEXT PRIMARY KEY,
                    value_json TEXT NOT NULL
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

    def _fetchone(self, sql: str, params: tuple[Any, ...] = ()) -> sqlite3.Row | None:
        with self._lock:
            return self._conn.execute(sql, params).fetchone()

    def _fetchall(self, sql: str, params: tuple[Any, ...] = ()) -> list[sqlite3.Row]:
        with self._lock:
            return list(self._conn.execute(sql, params).fetchall())
