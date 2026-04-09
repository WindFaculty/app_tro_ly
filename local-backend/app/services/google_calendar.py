from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime, time, timedelta
from typing import Any, Protocol
from urllib.parse import quote, urlencode

import httpx

from app.core.config import Settings
from app.core.ids import make_id
from app.core.time import iso_date, iso_datetime, now_local, parse_date, parse_datetime
from app.db.repository import SQLiteRepository
from app.models.schemas import (
    GoogleCalendarConnectResponse,
    GoogleCalendarDeleteResponse,
    GoogleCalendarEventCreateRequest,
    GoogleCalendarEventListResponse,
    GoogleCalendarEventRecord,
    GoogleCalendarEventUpdateRequest,
    GoogleCalendarStatusResponse,
)
from app.services.settings import SettingsService


GOOGLE_CALENDAR_PROVIDER = "google_calendar"
GOOGLE_CALENDAR_SCOPES = [
    "https://www.googleapis.com/auth/calendar",
]


class GoogleCalendarGatewayProtocol(Protocol):
    def build_authorization_url(
        self,
        *,
        client_id: str,
        redirect_uri: str,
        state: str,
        scopes: list[str],
    ) -> str: ...

    def exchange_code(
        self,
        *,
        client_id: str,
        client_secret: str,
        code: str,
        redirect_uri: str,
    ) -> dict[str, Any]: ...

    def refresh_access_token(
        self,
        *,
        client_id: str,
        client_secret: str,
        refresh_token: str,
    ) -> dict[str, Any]: ...

    def get_primary_calendar(self, *, access_token: str) -> dict[str, Any]: ...

    def list_events(
        self,
        *,
        access_token: str,
        calendar_id: str,
        time_min: str,
        time_max: str,
        query: str,
        max_results: int,
    ) -> dict[str, Any]: ...

    def create_event(
        self,
        *,
        access_token: str,
        calendar_id: str,
        payload: dict[str, Any],
    ) -> dict[str, Any]: ...

    def update_event(
        self,
        *,
        access_token: str,
        calendar_id: str,
        event_id: str,
        payload: dict[str, Any],
    ) -> dict[str, Any]: ...

    def delete_event(
        self,
        *,
        access_token: str,
        calendar_id: str,
        event_id: str,
    ) -> None: ...


class GoogleCalendarGateway:
    def __init__(self, timeout_sec: float = 15.0) -> None:
        self._timeout = timeout_sec

    def build_authorization_url(
        self,
        *,
        client_id: str,
        redirect_uri: str,
        state: str,
        scopes: list[str],
    ) -> str:
        params = urlencode(
            {
                "client_id": client_id,
                "redirect_uri": redirect_uri,
                "response_type": "code",
                "scope": " ".join(scopes),
                "access_type": "offline",
                "include_granted_scopes": "true",
                "prompt": "consent",
                "state": state,
            }
        )
        return f"https://accounts.google.com/o/oauth2/v2/auth?{params}"

    def exchange_code(
        self,
        *,
        client_id: str,
        client_secret: str,
        code: str,
        redirect_uri: str,
    ) -> dict[str, Any]:
        return self._token_request(
            {
                "client_id": client_id,
                "client_secret": client_secret,
                "code": code,
                "grant_type": "authorization_code",
                "redirect_uri": redirect_uri,
            }
        )

    def refresh_access_token(
        self,
        *,
        client_id: str,
        client_secret: str,
        refresh_token: str,
    ) -> dict[str, Any]:
        return self._token_request(
            {
                "client_id": client_id,
                "client_secret": client_secret,
                "refresh_token": refresh_token,
                "grant_type": "refresh_token",
            }
        )

    def get_primary_calendar(self, *, access_token: str) -> dict[str, Any]:
        return self._request_json(
            "GET",
            "https://www.googleapis.com/calendar/v3/users/me/calendarList/primary",
            access_token=access_token,
        )

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
        params: dict[str, Any] = {
            "timeMin": time_min,
            "timeMax": time_max,
            "singleEvents": "true",
            "orderBy": "startTime",
            "maxResults": max_results,
        }
        normalized_query = query.strip()
        if normalized_query:
            params["q"] = normalized_query
        return self._request_json(
            "GET",
            f"https://www.googleapis.com/calendar/v3/calendars/{quote(calendar_id, safe='')}/events",
            access_token=access_token,
            params=params,
        )

    def create_event(
        self,
        *,
        access_token: str,
        calendar_id: str,
        payload: dict[str, Any],
    ) -> dict[str, Any]:
        return self._request_json(
            "POST",
            f"https://www.googleapis.com/calendar/v3/calendars/{quote(calendar_id, safe='')}/events",
            access_token=access_token,
            json=payload,
        )

    def update_event(
        self,
        *,
        access_token: str,
        calendar_id: str,
        event_id: str,
        payload: dict[str, Any],
    ) -> dict[str, Any]:
        return self._request_json(
            "PUT",
            f"https://www.googleapis.com/calendar/v3/calendars/{quote(calendar_id, safe='')}/events/{quote(event_id, safe='')}",
            access_token=access_token,
            json=payload,
        )

    def delete_event(
        self,
        *,
        access_token: str,
        calendar_id: str,
        event_id: str,
    ) -> None:
        self._request_json(
            "DELETE",
            f"https://www.googleapis.com/calendar/v3/calendars/{quote(calendar_id, safe='')}/events/{quote(event_id, safe='')}",
            access_token=access_token,
            allow_empty=True,
        )

    def _token_request(self, payload: dict[str, Any]) -> dict[str, Any]:
        return self._request_json(
            "POST",
            "https://oauth2.googleapis.com/token",
            data=payload,
            include_auth=False,
        )

    def _request_json(
        self,
        method: str,
        url: str,
        *,
        access_token: str | None = None,
        include_auth: bool = True,
        allow_empty: bool = False,
        **kwargs: Any,
    ) -> dict[str, Any]:
        headers = dict(kwargs.pop("headers", {}))
        if include_auth and access_token:
            headers["Authorization"] = f"Bearer {access_token}"
        try:
            with httpx.Client(timeout=self._timeout) as client:
                response = client.request(method, url, headers=headers, **kwargs)
        except httpx.HTTPError as exc:
            raise RuntimeError(f"Google Calendar request failed: {exc}") from exc

        if response.is_error:
            detail = response.text.strip() or f"{response.status_code} {response.reason_phrase}"
            raise RuntimeError(f"Google Calendar request failed: {detail}")
        if allow_empty and not response.content:
            return {}
        return response.json() if response.content else {}


@dataclass
class GoogleCalendarTokenSnapshot:
    access_token: str | None
    refresh_token: str | None
    token_type: str | None
    scope: list[str]
    expires_at: str | None
    calendar_id: str | None
    last_sync_at: str | None
    last_error: str | None
    created_at: str
    updated_at: str


class GoogleCalendarService:
    def __init__(
        self,
        repository: SQLiteRepository,
        settings: Settings,
        settings_service: SettingsService,
        gateway: GoogleCalendarGatewayProtocol | None = None,
    ) -> None:
        self._repository = repository
        self._settings = settings
        self._settings_service = settings_service
        self._gateway = gateway or GoogleCalendarGateway(timeout_sec=settings.gmail_timeout_sec)

    def status(self) -> GoogleCalendarStatusResponse:
        prefs = self._prefs()
        token = self._token_snapshot()
        configured = bool(
            (self._settings.google_oauth_client_id or "").strip()
            and (self._settings.google_oauth_client_secret or "").strip()
        )
        connected = token is not None and bool(token.refresh_token or token.access_token)
        sync_enabled = bool(prefs.get("sync_enabled", True))
        detail = "Google Calendar integration is ready."
        status = "ready"
        if not configured:
            status = "not_configured"
            detail = "Set backend Google OAuth credentials to enable Google Calendar."
        elif not connected:
            status = "disconnected"
            detail = "Google Calendar account is not connected yet."
        elif not sync_enabled:
            status = "disabled"
            detail = "Google Calendar is connected, but sync is disabled in Settings."
        elif token and token.last_error:
            status = "error"
            detail = token.last_error

        return GoogleCalendarStatusResponse(
            status=status,
            configured=configured,
            connected=connected,
            sync_enabled=sync_enabled,
            calendar_id=token.calendar_id if token else None,
            auth_url_available=configured,
            redirect_uri=self._settings.google_calendar_oauth_redirect_uri if configured else None,
            default_calendar_id=self._resolve_calendar_id(str(prefs.get("default_calendar_id") or "primary")),
            agenda_days=self._normalize_days(prefs.get("agenda_days")),
            event_limit=self._normalize_event_limit(prefs.get("event_limit")),
            scopes=list(GOOGLE_CALENDAR_SCOPES),
            last_sync_at=token.last_sync_at if token else None,
            last_error=token.last_error if token else None,
            detail=detail,
        )

    def start_google_connect(self) -> GoogleCalendarConnectResponse:
        self._require_oauth_configured()
        state = make_id("calendar-auth")
        self._repository.set_google_oauth_state(
            GOOGLE_CALENDAR_PROVIDER,
            state,
            iso_datetime(now_local()),
        )
        authorization_url = self._gateway.build_authorization_url(
            client_id=self._settings.google_oauth_client_id or "",
            redirect_uri=self._settings.google_calendar_oauth_redirect_uri,
            state=state,
            scopes=list(GOOGLE_CALENDAR_SCOPES),
        )
        return GoogleCalendarConnectResponse(
            authorization_url=authorization_url,
            redirect_uri=self._settings.google_calendar_oauth_redirect_uri,
            state=state,
        )

    def complete_google_connect(self, code: str, state: str) -> GoogleCalendarStatusResponse:
        self._require_oauth_configured()
        expected_state = self._repository.get_google_oauth_state(GOOGLE_CALENDAR_PROVIDER)
        if expected_state is None or expected_state.get("state") != state:
            raise ValueError("Google OAuth state did not match the active calendar login request.")

        token_payload = self._gateway.exchange_code(
            client_id=self._settings.google_oauth_client_id or "",
            client_secret=self._settings.google_oauth_client_secret or "",
            code=code,
            redirect_uri=self._settings.google_calendar_oauth_redirect_uri,
        )
        access_token = str(token_payload.get("access_token") or "").strip()
        if not access_token:
            raise RuntimeError("Google OAuth did not return a calendar access token.")
        primary_calendar = self._gateway.get_primary_calendar(access_token=access_token)
        calendar_id = str(primary_calendar.get("id") or "").strip() or None
        existing = self._token_snapshot()
        self._persist_token(
            token_payload,
            calendar_id=calendar_id,
            refresh_token=(
                str(token_payload.get("refresh_token") or "").strip()
                or (existing.refresh_token if existing else None)
            ),
            last_error=None,
        )
        self._repository.clear_google_oauth_state(GOOGLE_CALENDAR_PROVIDER)
        return self.status()

    def disconnect_google_account(self) -> GoogleCalendarStatusResponse:
        self._repository.clear_google_oauth_token(GOOGLE_CALENDAR_PROVIDER)
        self._repository.clear_google_oauth_state(GOOGLE_CALENDAR_PROVIDER)
        return self.status()

    def list_events(
        self,
        *,
        start_date: str | None = None,
        days: int | None = None,
        calendar_id: str | None = None,
        query: str = "",
        limit: int | None = None,
    ) -> GoogleCalendarEventListResponse:
        account = self.status()
        resolved_start = parse_date(start_date) or now_local().date()
        resolved_days = self._normalize_days(days or account.agenda_days)
        resolved_end = resolved_start + timedelta(days=resolved_days - 1)
        resolved_calendar_id = self._resolve_calendar_id(calendar_id or account.default_calendar_id)
        if account.status in {"not_configured", "disconnected", "disabled"}:
            return GoogleCalendarEventListResponse(
                account=account,
                calendar_id=resolved_calendar_id,
                start_date=iso_date(resolved_start) or "",
                end_date=iso_date(resolved_end) or "",
                query=query.strip(),
                time_zone=None,
                items=[],
                count=0,
            )

        access_token = self._get_access_token()
        try:
            window_start = datetime.combine(resolved_start, time.min).astimezone().isoformat(timespec="seconds")
            window_end = datetime.combine(
                resolved_end + timedelta(days=1),
                time.min,
            ).astimezone().isoformat(timespec="seconds")
            payload = self._gateway.list_events(
                access_token=access_token,
                calendar_id=resolved_calendar_id,
                time_min=window_start,
                time_max=window_end,
                query=query.strip(),
                max_results=self._normalize_event_limit(limit or account.event_limit),
            )
            items = [
                self._event_record_from_payload(item, calendar_id=resolved_calendar_id)
                for item in payload.get("items") or []
            ]
            self._mark_sync_success()
        except RuntimeError as exc:
            self._mark_sync_error(str(exc))
            raise

        return GoogleCalendarEventListResponse(
            account=self.status(),
            calendar_id=resolved_calendar_id,
            start_date=iso_date(resolved_start) or "",
            end_date=iso_date(resolved_end) or "",
            query=query.strip(),
            time_zone=str(payload.get("timeZone") or "").strip() or None,
            items=items,
            count=len(items),
        )

    def create_event(self, request: GoogleCalendarEventCreateRequest) -> GoogleCalendarEventRecord:
        account = self.status()
        if account.status in {"not_configured", "disconnected", "disabled"}:
            raise RuntimeError(account.detail)
        access_token = self._get_access_token()
        resolved_calendar_id = self._resolve_calendar_id(request.calendar_id or account.default_calendar_id)
        time_zone = self._get_calendar_time_zone(access_token)
        try:
            payload = self._gateway.create_event(
                access_token=access_token,
                calendar_id=resolved_calendar_id,
                payload=self._build_event_payload(request, time_zone=time_zone),
            )
            self._mark_sync_success()
            return self._event_record_from_payload(payload, calendar_id=resolved_calendar_id)
        except RuntimeError as exc:
            self._mark_sync_error(str(exc))
            raise

    def update_event(
        self,
        event_id: str,
        request: GoogleCalendarEventUpdateRequest,
    ) -> GoogleCalendarEventRecord:
        account = self.status()
        if account.status in {"not_configured", "disconnected", "disabled"}:
            raise RuntimeError(account.detail)
        payload = request.model_dump(exclude_unset=True)
        if not payload:
            raise ValueError("Calendar update payload must include at least one field.")

        access_token = self._get_access_token()
        resolved_calendar_id = self._resolve_calendar_id(
            str(payload.get("calendar_id") or account.default_calendar_id)
        )
        time_zone = self._get_calendar_time_zone(access_token)
        try:
            response = self._gateway.update_event(
                access_token=access_token,
                calendar_id=resolved_calendar_id,
                event_id=event_id,
                payload=self._build_event_payload(request, time_zone=time_zone, partial=True),
            )
            self._mark_sync_success()
            return self._event_record_from_payload(response, calendar_id=resolved_calendar_id)
        except RuntimeError as exc:
            self._mark_sync_error(str(exc))
            raise

    def delete_event(
        self,
        event_id: str,
        calendar_id: str | None = None,
    ) -> GoogleCalendarDeleteResponse:
        account = self.status()
        if account.status in {"not_configured", "disconnected", "disabled"}:
            raise RuntimeError(account.detail)
        access_token = self._get_access_token()
        resolved_calendar_id = self._resolve_calendar_id(calendar_id or account.default_calendar_id)
        try:
            self._gateway.delete_event(
                access_token=access_token,
                calendar_id=resolved_calendar_id,
                event_id=event_id,
            )
            self._mark_sync_success()
            return GoogleCalendarDeleteResponse(
                status="deleted",
                event_id=event_id,
                calendar_id=resolved_calendar_id,
            )
        except RuntimeError as exc:
            self._mark_sync_error(str(exc))
            raise

    def _prefs(self) -> dict[str, Any]:
        settings = self._settings_service.get()
        group = settings.get("google_calendar")
        return group if isinstance(group, dict) else {}

    def _token_snapshot(self) -> GoogleCalendarTokenSnapshot | None:
        token = self._repository.get_google_oauth_token(GOOGLE_CALENDAR_PROVIDER)
        if token is None:
            return None
        return GoogleCalendarTokenSnapshot(
            access_token=token.get("access_token"),
            refresh_token=token.get("refresh_token"),
            token_type=token.get("token_type"),
            scope=list(token.get("scope") or []),
            expires_at=token.get("expires_at"),
            calendar_id=token.get("email_address"),
            last_sync_at=token.get("last_sync_at"),
            last_error=token.get("last_error"),
            created_at=str(token.get("created_at") or ""),
            updated_at=str(token.get("updated_at") or ""),
        )

    def _require_oauth_configured(self) -> None:
        if not (self._settings.google_oauth_client_id and self._settings.google_oauth_client_secret):
            raise ValueError("Google OAuth credentials are not configured in the backend environment.")

    def _get_access_token(self) -> str:
        token = self._token_snapshot()
        if token is None:
            raise RuntimeError("Google Calendar account is not connected.")
        if token.access_token and not self._token_is_expired(token):
            return token.access_token
        if not token.refresh_token:
            raise RuntimeError("Google Calendar connection is missing a refresh token.")
        self._require_oauth_configured()
        refreshed = self._gateway.refresh_access_token(
            client_id=self._settings.google_oauth_client_id or "",
            client_secret=self._settings.google_oauth_client_secret or "",
            refresh_token=token.refresh_token,
        )
        self._persist_token(
            refreshed,
            calendar_id=token.calendar_id,
            refresh_token=token.refresh_token,
            last_error=None,
        )
        latest = self._token_snapshot()
        if latest is None or not latest.access_token:
            raise RuntimeError("Google Calendar refresh did not produce a usable access token.")
        return latest.access_token

    def _persist_token(
        self,
        token_payload: dict[str, Any],
        *,
        calendar_id: str | None,
        refresh_token: str | None,
        last_error: str | None,
    ) -> None:
        now_iso = iso_datetime(now_local())
        expires_in = token_payload.get("expires_in")
        expires_at = None
        if isinstance(expires_in, (int, float)):
            expires_at = iso_datetime(now_local() + timedelta(seconds=int(expires_in)))
        existing = self._token_snapshot()
        self._repository.upsert_google_oauth_token(
            {
                "provider": GOOGLE_CALENDAR_PROVIDER,
                "access_token": str(token_payload.get("access_token") or "").strip() or None,
                "refresh_token": refresh_token,
                "token_type": str(token_payload.get("token_type") or "Bearer"),
                "scope": self._scope_list(token_payload.get("scope"), existing.scope if existing else []),
                "expires_at": expires_at,
                "email_address": calendar_id,
                "last_sync_at": existing.last_sync_at if existing else None,
                "last_error": last_error,
                "created_at": existing.created_at if existing else now_iso,
                "updated_at": now_iso,
            }
        )

    def _mark_sync_success(self) -> None:
        token = self._token_snapshot()
        if token is None:
            return
        now_iso = iso_datetime(now_local())
        self._repository.upsert_google_oauth_token(
            {
                "provider": GOOGLE_CALENDAR_PROVIDER,
                "access_token": token.access_token,
                "refresh_token": token.refresh_token,
                "token_type": token.token_type,
                "scope": token.scope,
                "expires_at": token.expires_at,
                "email_address": token.calendar_id,
                "last_sync_at": now_iso,
                "last_error": None,
                "created_at": token.created_at,
                "updated_at": now_iso,
            }
        )

    def _mark_sync_error(self, error_text: str) -> None:
        token = self._token_snapshot()
        if token is None:
            return
        self._repository.upsert_google_oauth_token(
            {
                "provider": GOOGLE_CALENDAR_PROVIDER,
                "access_token": token.access_token,
                "refresh_token": token.refresh_token,
                "token_type": token.token_type,
                "scope": token.scope,
                "expires_at": token.expires_at,
                "email_address": token.calendar_id,
                "last_sync_at": token.last_sync_at,
                "last_error": error_text,
                "created_at": token.created_at,
                "updated_at": iso_datetime(now_local()),
            }
        )

    def _build_event_payload(
        self,
        request: GoogleCalendarEventCreateRequest | GoogleCalendarEventUpdateRequest,
        *,
        time_zone: str | None,
        partial: bool = False,
    ) -> dict[str, Any]:
        payload = request.model_dump(exclude_unset=partial)
        event_payload: dict[str, Any] = {}
        for field in ("summary", "description", "location"):
            if field in payload:
                event_payload[field] = payload.get(field) or ""
        if "attendees" in payload:
            event_payload["attendees"] = [{"email": attendee} for attendee in payload.get("attendees") or []]

        time_fields = {"is_all_day", "start_at", "end_at", "start_date", "end_date"}
        if not partial or time_fields.intersection(payload.keys()):
            event_payload.update(self._build_event_times(request, time_zone=time_zone))

        if not event_payload:
            raise ValueError("Calendar payload did not contain any writable fields.")
        return event_payload

    def _build_event_times(
        self,
        request: GoogleCalendarEventCreateRequest | GoogleCalendarEventUpdateRequest,
        *,
        time_zone: str | None,
    ) -> dict[str, Any]:
        if request.is_all_day:
            start_day = parse_date(request.start_date)
            end_day = parse_date(request.end_date or request.start_date)
            if start_day is None:
                raise ValueError("All-day calendar events require a start_date.")
            if end_day is None:
                raise ValueError("All-day calendar events require an end_date.")
            if end_day < start_day:
                raise ValueError("All-day calendar events must end on or after the start date.")
            return {
                "start": {"date": iso_date(start_day)},
                "end": {"date": iso_date(end_day + timedelta(days=1))},
            }

        start_at = (request.start_at or "").strip()
        end_at = (request.end_at or "").strip()
        if not start_at or not end_at:
            raise ValueError("Timed calendar events require both start_at and end_at.")
        parsed_start = parse_datetime(start_at)
        parsed_end = parse_datetime(end_at)
        if parsed_start is None or parsed_end is None:
            raise ValueError("Timed calendar events must use ISO datetime values.")
        if parsed_end <= parsed_start:
            raise ValueError("Timed calendar events must end after they start.")

        start_payload: dict[str, Any] = {"dateTime": start_at}
        end_payload: dict[str, Any] = {"dateTime": end_at}
        if time_zone and not self._has_timezone(start_at) and not self._has_timezone(end_at):
            start_payload["timeZone"] = time_zone
            end_payload["timeZone"] = time_zone
        return {"start": start_payload, "end": end_payload}

    def _event_record_from_payload(
        self,
        payload: dict[str, Any],
        *,
        calendar_id: str,
    ) -> GoogleCalendarEventRecord:
        start_payload = payload.get("start") or {}
        end_payload = payload.get("end") or {}
        is_all_day = "date" in start_payload
        start_date = str(start_payload.get("date") or "").strip() or None
        end_date = str(end_payload.get("date") or "").strip() or None
        if is_all_day and end_date:
            parsed_end_date = parse_date(end_date)
            end_date = iso_date(parsed_end_date - timedelta(days=1)) if parsed_end_date else end_date
        return GoogleCalendarEventRecord(
            id=str(payload.get("id") or ""),
            calendar_id=calendar_id,
            status=str(payload.get("status") or "confirmed"),
            summary=str(payload.get("summary") or "(Untitled event)"),
            description=str(payload.get("description") or "").strip() or None,
            location=str(payload.get("location") or "").strip() or None,
            html_link=str(payload.get("htmlLink") or "").strip() or None,
            conference_link=self._conference_link(payload),
            organizer_email=self._payload_email(payload.get("organizer")),
            creator_email=self._payload_email(payload.get("creator")),
            attendees=self._attendee_list(payload.get("attendees") or []),
            start_at=str(start_payload.get("dateTime") or "").strip() or None,
            end_at=str(end_payload.get("dateTime") or "").strip() or None,
            start_date=start_date,
            end_date=end_date or start_date,
            is_all_day=is_all_day,
            created_at=str(payload.get("created") or "").strip() or None,
            updated_at=str(payload.get("updated") or "").strip() or None,
        )

    def _conference_link(self, payload: dict[str, Any]) -> str | None:
        hangout_link = str(payload.get("hangoutLink") or "").strip()
        if hangout_link:
            return hangout_link
        conference_data = payload.get("conferenceData") or {}
        for entry in conference_data.get("entryPoints") or []:
            uri = str(entry.get("uri") or "").strip()
            if uri:
                return uri
        return None

    def _payload_email(self, payload: Any) -> str | None:
        if not isinstance(payload, dict):
            return None
        return str(payload.get("email") or "").strip() or None

    def _attendee_list(self, attendees: list[dict[str, Any]]) -> list[str]:
        items: list[str] = []
        seen: set[str] = set()
        for attendee in attendees:
            email = str(attendee.get("email") or "").strip()
            key = email.casefold()
            if email and key not in seen:
                items.append(email)
                seen.add(key)
        return items

    def _get_calendar_time_zone(self, access_token: str) -> str | None:
        try:
            payload = self._gateway.get_primary_calendar(access_token=access_token)
        except RuntimeError:
            return None
        return str(payload.get("timeZone") or "").strip() or None

    def _resolve_calendar_id(self, value: str | None) -> str:
        cleaned = (value or "primary").strip()
        return cleaned or "primary"

    def _normalize_event_limit(self, value: Any) -> int:
        try:
            numeric = int(value)
        except (TypeError, ValueError):
            numeric = 20
        return min(max(numeric, 1), 100)

    def _normalize_days(self, value: Any) -> int:
        try:
            numeric = int(value)
        except (TypeError, ValueError):
            numeric = 7
        return min(max(numeric, 1), 31)

    def _scope_list(self, value: Any, fallback: list[str]) -> list[str]:
        if isinstance(value, str):
            scopes = [item.strip() for item in value.split() if item.strip()]
            return scopes or list(fallback)
        if isinstance(value, list):
            scopes = [str(item).strip() for item in value if str(item).strip()]
            return scopes or list(fallback)
        return list(fallback)

    def _token_is_expired(self, token: GoogleCalendarTokenSnapshot) -> bool:
        expires_at = parse_datetime(token.expires_at)
        if expires_at is None:
            return False
        return expires_at <= now_local() + timedelta(seconds=60)

    def _has_timezone(self, value: str) -> bool:
        if value.endswith("Z"):
            return True
        if len(value) < 6:
            return False
        suffix = value[-6:]
        return suffix[0] in {"+", "-"} and suffix[3] == ":"
