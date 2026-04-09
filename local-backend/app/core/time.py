from __future__ import annotations

from calendar import monthrange
from datetime import date, datetime, time, timedelta


def now_local() -> datetime:
    return datetime.now().replace(microsecond=0)


def _normalize_datetime(value: datetime) -> datetime:
    trimmed = value.replace(microsecond=0)
    if trimmed.tzinfo is None or trimmed.utcoffset() is None:
        return trimmed
    return trimmed.astimezone().replace(tzinfo=None)


def iso_datetime(value: datetime | None) -> str | None:
    if value is None:
        return None
    return _normalize_datetime(value).isoformat()


def iso_date(value: date | None) -> str | None:
    if value is None:
        return None
    return value.isoformat()


def parse_date(value: str | None) -> date | None:
    if not value:
        return None
    return date.fromisoformat(value)


def parse_datetime(value: str | None) -> datetime | None:
    if not value:
        return None
    return _normalize_datetime(datetime.fromisoformat(value.replace("Z", "+00:00")))


def combine_date_time(day: date, value: time | None) -> datetime | None:
    if value is None:
        return None
    return datetime.combine(day, value)


def add_month(day: date) -> date:
    month = day.month + 1
    year = day.year
    if month == 13:
        month = 1
        year += 1
    new_day = min(day.day, monthrange(year, month)[1])
    return date(year, month, new_day)


def daterange(start: date, end: date) -> list[date]:
    delta = (end - start).days
    return [start + timedelta(days=offset) for offset in range(delta + 1)]
