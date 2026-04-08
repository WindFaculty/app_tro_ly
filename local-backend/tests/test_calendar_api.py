from __future__ import annotations

from datetime import date, datetime, timedelta
from typing import Any


def _timed_event(
    *,
    event_id: str,
    summary: str,
    start_at: str,
    end_at: str,
    location: str = "",
    description: str = "",
) -> dict[str, Any]:
    return {
        "id": event_id,
        "status": "confirmed",
        "summary": summary,
        "description": description,
        "location": location,
        "htmlLink": f"https://calendar.google.com/calendar/event?eid={event_id}",
        "organizer": {"email": "primary@example.com"},
        "creator": {"email": "primary@example.com"},
        "attendees": [{"email": "teammate@example.com"}],
        "start": {"dateTime": start_at, "timeZone": "Asia/Bangkok"},
        "end": {"dateTime": end_at, "timeZone": "Asia/Bangkok"},
        "created": "2026-04-01T08:00:00+07:00",
        "updated": "2026-04-01T08:00:00+07:00",
    }


def _all_day_event(
    *,
    event_id: str,
    summary: str,
    start_date: str,
    end_date: str,
) -> dict[str, Any]:
    end_exclusive = (date.fromisoformat(end_date) + timedelta(days=1)).isoformat()
    return {
        "id": event_id,
        "status": "confirmed",
        "summary": summary,
        "htmlLink": f"https://calendar.google.com/calendar/event?eid={event_id}",
        "organizer": {"email": "primary@example.com"},
        "creator": {"email": "primary@example.com"},
        "start": {"date": start_date},
        "end": {"date": end_exclusive},
        "created": "2026-04-01T08:00:00+07:00",
        "updated": "2026-04-01T08:00:00+07:00",
    }


class FakeGoogleCalendarGateway:
    def __init__(self) -> None:
        self._events = {
            "evt-1": _timed_event(
                event_id="evt-1",
                summary="Planning review",
                start_at="2026-04-07T09:00:00+07:00",
                end_at="2026-04-07T10:00:00+07:00",
                location="Studio",
                description="Review launch scope.",
            ),
            "evt-2": _all_day_event(
                event_id="evt-2",
                summary="Company holiday",
                start_date="2026-04-09",
                end_date="2026-04-09",
            ),
        }

    def build_authorization_url(
        self,
        *,
        client_id: str,
        redirect_uri: str,
        state: str,
        scopes: list[str],
    ) -> str:
        return (
            "https://accounts.google.com/o/oauth2/v2/auth"
            f"?state={state}&client_id={client_id}&redirect_uri={redirect_uri}"
        )

    def exchange_code(
        self,
        *,
        client_id: str,
        client_secret: str,
        code: str,
        redirect_uri: str,
    ) -> dict[str, Any]:
        return {
            "access_token": f"calendar-token-{code}",
            "refresh_token": "calendar-refresh-token",
            "token_type": "Bearer",
            "expires_in": 3600,
            "scope": "https://www.googleapis.com/auth/calendar",
        }

    def refresh_access_token(
        self,
        *,
        client_id: str,
        client_secret: str,
        refresh_token: str,
    ) -> dict[str, Any]:
        return {
            "access_token": "calendar-refreshed-token",
            "token_type": "Bearer",
            "expires_in": 3600,
            "scope": "https://www.googleapis.com/auth/calendar",
        }

    def get_primary_calendar(self, *, access_token: str) -> dict[str, Any]:
        return {
            "id": "primary@example.com",
            "summary": "Primary calendar",
            "timeZone": "Asia/Bangkok",
        }

    def list_events(
        self,
        *,
        access_token: str,
        calendar_id: str,
        time_min: str,
        time_max: str,
        query: str,
        max_results: int,
    ) -> dict[str, Any]:
        normalized_query = query.strip().lower()
        window_start = datetime.fromisoformat(time_min)
        window_end = datetime.fromisoformat(time_max)

        def normalize(value: datetime) -> datetime:
            return value.astimezone().replace(tzinfo=None) if value.tzinfo else value

        items: list[dict[str, Any]] = []
        for payload in self._events.values():
            if normalized_query and normalized_query not in str(payload.get("summary") or "").lower():
                continue
            event_start = payload.get("start") or {}
            if "dateTime" in event_start:
                anchor = normalize(
                    datetime.fromisoformat(str(event_start["dateTime"]).replace("Z", "+00:00"))
                )
            else:
                anchor = datetime.fromisoformat(f"{event_start['date']}T00:00:00")
            if not (normalize(window_start) <= anchor < normalize(window_end)):
                continue
            items.append(payload)
        items.sort(key=lambda item: str((item.get("start") or {}).get("dateTime") or (item.get("start") or {}).get("date")))
        return {"timeZone": "Asia/Bangkok", "items": items[:max_results]}

    def create_event(
        self,
        *,
        access_token: str,
        calendar_id: str,
        payload: dict[str, Any],
    ) -> dict[str, Any]:
        event_id = f"evt-{len(self._events) + 1}"
        created = {
            "id": event_id,
            "status": "confirmed",
            "summary": payload.get("summary") or "(Untitled event)",
            "description": payload.get("description") or "",
            "location": payload.get("location") or "",
            "htmlLink": f"https://calendar.google.com/calendar/event?eid={event_id}",
            "organizer": {"email": "primary@example.com"},
            "creator": {"email": "primary@example.com"},
            "attendees": payload.get("attendees") or [],
            "start": payload["start"],
            "end": payload["end"],
            "created": "2026-04-01T08:00:00+07:00",
            "updated": "2026-04-02T08:00:00+07:00",
        }
        self._events[event_id] = created
        return created

    def update_event(
        self,
        *,
        access_token: str,
        calendar_id: str,
        event_id: str,
        payload: dict[str, Any],
    ) -> dict[str, Any]:
        current = dict(self._events[event_id])
        if "summary" in payload:
            current["summary"] = payload["summary"]
        if "description" in payload:
            current["description"] = payload["description"]
        if "location" in payload:
            current["location"] = payload["location"]
        if "attendees" in payload:
            current["attendees"] = payload["attendees"]
        if "start" in payload:
            current["start"] = payload["start"]
        if "end" in payload:
            current["end"] = payload["end"]
        current["updated"] = "2026-04-03T08:00:00+07:00"
        self._events[event_id] = current
        return current

    def delete_event(
        self,
        *,
        access_token: str,
        calendar_id: str,
        event_id: str,
    ) -> None:
        self._events.pop(event_id, None)


def _install_fake_calendar(client) -> FakeGoogleCalendarGateway:
    gateway = FakeGoogleCalendarGateway()
    container = client.app.state.container
    container.settings.google_oauth_client_id = "client-id"
    container.settings.google_oauth_client_secret = "client-secret"
    container.google_calendar_service._gateway = gateway
    container.google_calendar_service.disconnect_google_account()
    return gateway


def _connect_google_calendar(client) -> FakeGoogleCalendarGateway:
    gateway = _install_fake_calendar(client)
    start = client.get("/v1/calendar/google/connect")
    assert start.status_code == 200
    state = start.json()["state"]
    callback = client.get("/v1/calendar/google/callback", params={"code": "demo-code", "state": state})
    assert callback.status_code == 200
    return gateway


def test_calendar_events_stay_empty_when_google_is_not_configured(client) -> None:
    response = client.get("/v1/calendar/events", params={"start_date": "2026-04-07", "days": 7})
    assert response.status_code == 200
    payload = response.json()
    assert payload["count"] == 0
    assert payload["account"]["status"] == "not_configured"
    assert payload["account"]["configured"] is False


def test_google_calendar_auth_flow_and_settings_roundtrip(client) -> None:
    gateway = _install_fake_calendar(client)

    status_before = client.get("/v1/calendar/status")
    assert status_before.status_code == 200
    assert status_before.json()["status"] == "disconnected"
    assert status_before.json()["configured"] is True

    connect = client.get("/v1/calendar/google/connect")
    assert connect.status_code == 200
    assert "state=" in connect.json()["authorization_url"]

    callback = client.get(
        "/v1/calendar/google/callback",
        params={"code": "demo-code", "state": connect.json()["state"]},
    )
    assert callback.status_code == 200
    assert "Google calendar connected" in callback.text

    status_after = client.get("/v1/calendar/status")
    assert status_after.status_code == 200
    payload = status_after.json()
    assert payload["status"] == "ready"
    assert payload["calendar_id"] == "primary@example.com"

    settings_update = client.put(
        "/v1/settings",
        json={
            "google_calendar": {
                "sync_enabled": False,
                "default_calendar_id": "team-calendar",
                "agenda_days": 14,
                "event_limit": 12,
            }
        },
    )
    assert settings_update.status_code == 200
    settings_payload = settings_update.json()
    assert settings_payload["google_calendar"]["sync_enabled"] is False
    assert settings_payload["google_calendar"]["default_calendar_id"] == "team-calendar"
    assert settings_payload["google_calendar"]["agenda_days"] == 14
    assert settings_payload["google_calendar"]["event_limit"] == 12

    disabled_status = client.get("/v1/calendar/status")
    assert disabled_status.status_code == 200
    assert disabled_status.json()["status"] == "disabled"

    disconnect = client.post("/v1/calendar/google/disconnect")
    assert disconnect.status_code == 200
    assert disconnect.json()["status"] == "disconnected"
    assert gateway.get_primary_calendar(access_token="ignored")["id"] == "primary@example.com"


def test_calendar_agenda_and_event_crud(client) -> None:
    _connect_google_calendar(client)

    agenda = client.get(
        "/v1/calendar/events",
        params={"start_date": "2026-04-07", "days": 7, "query": "planning", "limit": 10},
    )
    assert agenda.status_code == 200
    agenda_payload = agenda.json()
    assert agenda_payload["count"] == 1
    assert agenda_payload["time_zone"] == "Asia/Bangkok"
    assert agenda_payload["items"][0]["summary"] == "Planning review"

    full_agenda = client.get("/v1/calendar/events", params={"start_date": "2026-04-07", "days": 7})
    assert full_agenda.status_code == 200
    full_payload = full_agenda.json()
    assert full_payload["count"] == 2
    assert any(item["is_all_day"] for item in full_payload["items"])

    created = client.post(
        "/v1/calendar/events",
        json={
            "summary": "Project kickoff",
            "description": "Align owners and milestones.",
            "location": "Call room",
            "start_at": "2026-04-08T14:00:00",
            "end_at": "2026-04-08T15:00:00",
            "attendees": ["pm@example.com", "design@example.com"],
        },
    )
    assert created.status_code == 200
    created_payload = created.json()
    assert created_payload["summary"] == "Project kickoff"
    assert created_payload["location"] == "Call room"
    event_id = created_payload["id"]

    updated = client.put(
        f"/v1/calendar/events/{event_id}",
        json={"summary": "Project kickoff moved", "location": "Zoom"},
    )
    assert updated.status_code == 200
    updated_payload = updated.json()
    assert updated_payload["summary"] == "Project kickoff moved"
    assert updated_payload["location"] == "Zoom"

    deleted = client.delete(f"/v1/calendar/events/{event_id}", params={"calendar_id": "primary"})
    assert deleted.status_code == 200
    deleted_payload = deleted.json()
    assert deleted_payload["status"] == "deleted"
    assert deleted_payload["event_id"] == event_id

    after_delete = client.get("/v1/calendar/events", params={"start_date": "2026-04-07", "days": 7})
    assert after_delete.status_code == 200
    assert after_delete.json()["count"] == 2
