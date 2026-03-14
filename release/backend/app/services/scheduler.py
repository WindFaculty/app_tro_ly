from __future__ import annotations

import asyncio

from app.core.config import Settings
from app.core.events import EventBus
from app.core.logging import get_logger
from app.core.time import iso_datetime, now_local, parse_datetime
from app.db.repository import SQLiteRepository

logger = get_logger("scheduler")


class SchedulerService:
    def __init__(
        self,
        repository: SQLiteRepository,
        event_bus: EventBus,
        settings: Settings,
    ) -> None:
        self._repository = repository
        self._event_bus = event_bus
        self._settings = settings

    async def tick(self) -> int:
        now = now_local()
        due = self._repository.list_due_reminders(iso_datetime(now))
        for reminder in due:
            self._repository.mark_reminder_delivered(reminder["id"], iso_datetime(now))
            scheduled_for = reminder["start_at"] or reminder["due_at"]
            minutes_until = 0
            scheduled_dt = parse_datetime(scheduled_for)
            if scheduled_dt is not None:
                minutes_until = int((scheduled_dt - now).total_seconds() // 60)
            await self._event_bus.publish(
                {
                    "type": "reminder_due",
                    "task_id": reminder["task_id"],
                    "title": reminder["title"],
                    "scheduled_for": scheduled_for,
                    "minutes_until": minutes_until,
                }
            )
        if due:
            logger.info("Delivered %s due reminder(s)", len(due))
        return len(due)

    async def run(self, stop_event: asyncio.Event) -> None:
        while not stop_event.is_set():
            try:
                await self.tick()
            except Exception:
                logger.exception("Scheduler tick failed; continuing on next cycle")
            try:
                await asyncio.wait_for(stop_event.wait(), timeout=self._settings.reminder_poll_seconds)
            except asyncio.TimeoutError:
                continue
