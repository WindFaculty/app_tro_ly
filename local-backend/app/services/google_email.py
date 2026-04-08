from __future__ import annotations

import base64
import html
import re
from dataclasses import dataclass
from datetime import datetime, timedelta, timezone
from email.header import decode_header, make_header
from email.message import EmailMessage
from email.utils import getaddresses, parseaddr
from typing import Any, Protocol
from urllib.parse import urlencode

import httpx

from app.core.config import Settings
from app.core.ids import make_id
from app.core.time import iso_datetime, now_local, parse_datetime
from app.db.repository import SQLiteRepository
from app.models.schemas import (
    EmailDraftCreateRequest,
    EmailDraftListResponse,
    EmailDraftRecord,
    EmailDraftUpdateRequest,
    EmailMessageDetail,
    EmailMessageListResponse,
    EmailMessageSummary,
    EmailToTaskRequest,
    GoogleEmailConnectResponse,
    GoogleEmailStatusResponse,
    TaskCreateRequest,
    TaskRecord,
)
from app.services.settings import SettingsService
from app.services.tasks import TaskService


GMAIL_PROVIDER = "gmail"
GMAIL_SCOPES = [
    "https://www.googleapis.com/auth/gmail.modify",
    "https://www.googleapis.com/auth/gmail.send",
]


class GmailGatewayProtocol(Protocol):
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

    def get_profile(self, *, access_token: str) -> dict[str, Any]: ...

    def list_messages(
        self,
        *,
        access_token: str,
        query: str,
        label: str | None,
        max_results: int,
    ) -> list[dict[str, Any]]: ...

    def get_message_detail(self, *, access_token: str, message_id: str) -> dict[str, Any]: ...

    def send_message(self, *, access_token: str, raw_message: str) -> dict[str, Any]: ...


class GmailGateway:
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

    def get_profile(self, *, access_token: str) -> dict[str, Any]:
        return self._request_json(
            "GET",
            "https://gmail.googleapis.com/gmail/v1/users/me/profile",
            access_token=access_token,
        )

    def list_messages(
        self,
        *,
        access_token: str,
        query: str,
        label: str | None,
        max_results: int,
    ) -> list[dict[str, Any]]:
        params: dict[str, Any] = {"maxResults": max_results}
        normalized_query = query.strip()
        normalized_label = (label or "").strip().upper()
        if normalized_query:
            params["q"] = normalized_query
        if normalized_label and normalized_label not in {"ALL", "ANY"}:
            params["labelIds"] = normalized_label
        payload = self._request_json(
            "GET",
            "https://gmail.googleapis.com/gmail/v1/users/me/messages",
            access_token=access_token,
            params=params,
        )
        items = []
        for item in payload.get("messages") or []:
            items.append(
                self._request_json(
                    "GET",
                    f"https://gmail.googleapis.com/gmail/v1/users/me/messages/{item['id']}",
                    access_token=access_token,
                    params={
                        "format": "metadata",
                        "metadataHeaders": ["Subject", "From", "To", "Cc", "Date"],
                    },
                )
            )
        return items

    def get_message_detail(self, *, access_token: str, message_id: str) -> dict[str, Any]:
        return self._request_json(
            "GET",
            f"https://gmail.googleapis.com/gmail/v1/users/me/messages/{message_id}",
            access_token=access_token,
            params={"format": "full"},
        )

    def send_message(self, *, access_token: str, raw_message: str) -> dict[str, Any]:
        return self._request_json(
            "POST",
            "https://gmail.googleapis.com/gmail/v1/users/me/messages/send",
            access_token=access_token,
            json={"raw": raw_message},
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
        **kwargs: Any,
    ) -> dict[str, Any]:
        headers = dict(kwargs.pop("headers", {}))
        if include_auth and access_token:
            headers["Authorization"] = f"Bearer {access_token}"
        try:
            with httpx.Client(timeout=self._timeout) as client:
                response = client.request(method, url, headers=headers, **kwargs)
        except httpx.HTTPError as exc:
            raise RuntimeError(f"Gmail request failed: {exc}") from exc

        if response.is_error:
            detail = response.text.strip() or f"{response.status_code} {response.reason_phrase}"
            raise RuntimeError(f"Gmail request failed: {detail}")
        return response.json()


@dataclass
class GmailTokenSnapshot:
    access_token: str | None
    refresh_token: str | None
    token_type: str | None
    scope: list[str]
    expires_at: str | None
    email_address: str | None
    last_sync_at: str | None
    last_error: str | None
    created_at: str
    updated_at: str


class GoogleEmailService:
    def __init__(
        self,
        repository: SQLiteRepository,
        settings: Settings,
        settings_service: SettingsService,
        task_service: TaskService,
        gateway: GmailGatewayProtocol | None = None,
    ) -> None:
        self._repository = repository
        self._settings = settings
        self._settings_service = settings_service
        self._task_service = task_service
        self._gateway = gateway or GmailGateway(timeout_sec=settings.gmail_timeout_sec)

    def status(self) -> GoogleEmailStatusResponse:
        prefs = self._prefs()
        token = self._token_snapshot()
        configured = bool(
            (self._settings.google_oauth_client_id or "").strip()
            and (self._settings.google_oauth_client_secret or "").strip()
        )
        connected = token is not None and bool(token.refresh_token or token.access_token)
        sync_enabled = bool(prefs.get("sync_enabled", True))
        detail = "Google email integration is ready."
        status = "ready"
        if not configured:
            status = "not_configured"
            detail = "Set backend Google OAuth credentials to enable Gmail."
        elif not connected:
            status = "disconnected"
            detail = "Google email account is not connected yet."
        elif not sync_enabled:
            status = "disabled"
            detail = "Google email is connected, but sync is disabled in Settings."
        elif token and token.last_error:
            status = "error"
            detail = token.last_error

        return GoogleEmailStatusResponse(
            status=status,
            configured=configured,
            connected=connected,
            sync_enabled=sync_enabled,
            email_address=token.email_address if token else None,
            auth_url_available=configured,
            redirect_uri=self._settings.gmail_oauth_redirect_uri if configured else None,
            default_label=self._normalize_label(str(prefs.get("default_label") or "INBOX")),
            query_limit=self._normalize_limit(prefs.get("query_limit")),
            scopes=list(GMAIL_SCOPES),
            last_sync_at=token.last_sync_at if token else None,
            last_error=token.last_error if token else None,
            detail=detail,
        )

    def start_google_connect(self) -> GoogleEmailConnectResponse:
        self._require_oauth_configured()
        state = make_id("gmail-auth")
        self._repository.set_google_oauth_state(GMAIL_PROVIDER, state, iso_datetime(now_local()))
        authorization_url = self._gateway.build_authorization_url(
            client_id=self._settings.google_oauth_client_id or "",
            redirect_uri=self._settings.gmail_oauth_redirect_uri,
            state=state,
            scopes=list(GMAIL_SCOPES),
        )
        return GoogleEmailConnectResponse(
            authorization_url=authorization_url,
            redirect_uri=self._settings.gmail_oauth_redirect_uri,
            state=state,
        )

    def complete_google_connect(self, code: str, state: str) -> GoogleEmailStatusResponse:
        self._require_oauth_configured()
        expected_state = self._repository.get_google_oauth_state(GMAIL_PROVIDER)
        if expected_state is None or expected_state.get("state") != state:
            raise ValueError("Google OAuth state did not match the active login request.")

        token_payload = self._gateway.exchange_code(
            client_id=self._settings.google_oauth_client_id or "",
            client_secret=self._settings.google_oauth_client_secret or "",
            code=code,
            redirect_uri=self._settings.gmail_oauth_redirect_uri,
        )
        access_token = str(token_payload.get("access_token") or "").strip()
        if not access_token:
            raise RuntimeError("Google OAuth did not return an access token.")
        profile = self._gateway.get_profile(access_token=access_token)
        email_address = str(profile.get("emailAddress") or "").strip() or None
        existing = self._token_snapshot()
        self._persist_token(
            token_payload,
            email_address=email_address,
            refresh_token=(
                str(token_payload.get("refresh_token") or "").strip()
                or (existing.refresh_token if existing else None)
            ),
            last_error=None,
        )
        self._repository.clear_google_oauth_state(GMAIL_PROVIDER)
        return self.status()

    def disconnect_google_account(self) -> GoogleEmailStatusResponse:
        self._repository.clear_google_oauth_token(GMAIL_PROVIDER)
        self._repository.clear_google_oauth_state(GMAIL_PROVIDER)
        return self.status()

    def list_messages(
        self,
        *,
        query: str = "",
        label: str | None = None,
        limit: int | None = None,
    ) -> EmailMessageListResponse:
        account = self.status()
        draft_count = self.list_drafts(limit=100).count
        resolved_label = self._normalize_label(label or account.default_label)
        if account.status in {"not_configured", "disconnected", "disabled"}:
            return EmailMessageListResponse(
                account=account,
                query=query.strip(),
                label=resolved_label,
                items=[],
                count=0,
                draft_count=draft_count,
            )

        access_token = self._get_access_token()
        try:
            payloads = self._gateway.list_messages(
                access_token=access_token,
                query=query.strip(),
                label=resolved_label,
                max_results=self._normalize_limit(limit or account.query_limit),
            )
            items = self._decorate_messages(payloads)
            self._mark_sync_success()
        except RuntimeError as exc:
            self._mark_sync_error(str(exc))
            raise

        return EmailMessageListResponse(
            account=self.status(),
            query=query.strip(),
            label=resolved_label,
            items=items,
            count=len(items),
            draft_count=draft_count,
        )

    def get_message(self, message_id: str) -> EmailMessageDetail:
        account = self.status()
        if account.status in {"not_configured", "disconnected", "disabled"}:
            raise RuntimeError(account.detail)

        access_token = self._get_access_token()
        try:
            payload = self._gateway.get_message_detail(access_token=access_token, message_id=message_id)
            item = self._message_detail_from_payload(payload)
            linked = self._repository.list_email_task_links([message_id])
            item.linked_task_ids = linked.get(message_id, [])
            self._mark_sync_success()
            return item
        except RuntimeError as exc:
            self._mark_sync_error(str(exc))
            raise

    def list_drafts(self, limit: int = 50) -> EmailDraftListResponse:
        items = [
            EmailDraftRecord.model_validate(item)
            for item in self._repository.list_email_drafts(provider=GMAIL_PROVIDER, limit=limit)
        ]
        return EmailDraftListResponse(items=items, count=len(items))

    def create_draft(self, request: EmailDraftCreateRequest) -> EmailDraftRecord:
        now_iso = iso_datetime(now_local())
        payload = request.model_dump()
        payload.update(
            {
                "id": make_id("draft"),
                "provider": GMAIL_PROVIDER,
                "status": "draft",
                "gmail_message_id": None,
                "created_at": now_iso,
                "updated_at": now_iso,
                "sent_at": None,
            }
        )
        self._repository.create_email_draft(payload)
        return EmailDraftRecord.model_validate(self._repository.get_email_draft(payload["id"]))

    def update_draft(self, draft_id: str, request: EmailDraftUpdateRequest) -> EmailDraftRecord:
        existing = self._require_draft(draft_id)
        updates = request.model_dump(exclude_unset=True)
        if not updates:
            return EmailDraftRecord.model_validate(existing)
        merged = {**existing, **updates, "updated_at": iso_datetime(now_local())}
        self._repository.update_email_draft(draft_id, merged)
        return EmailDraftRecord.model_validate(self._require_draft(draft_id))

    def send_draft(self, draft_id: str) -> EmailDraftRecord:
        draft = self._require_draft(draft_id)
        if draft["status"] == "sent":
            return EmailDraftRecord.model_validate(draft)
        if not draft["to"]:
            raise ValueError("At least one recipient is required before sending an email draft.")
        account = self.status()
        if account.status in {"not_configured", "disconnected", "disabled"}:
            raise RuntimeError(account.detail)

        raw_message = self._compose_raw_message(draft)
        access_token = self._get_access_token()
        try:
            response = self._gateway.send_message(access_token=access_token, raw_message=raw_message)
            now_iso = iso_datetime(now_local())
            merged = {
                **draft,
                "status": "sent",
                "gmail_message_id": response.get("id"),
                "sent_at": now_iso,
                "updated_at": now_iso,
            }
            self._repository.update_email_draft(draft_id, merged)
            self._mark_sync_success()
            return EmailDraftRecord.model_validate(self._require_draft(draft_id))
        except RuntimeError as exc:
            self._mark_sync_error(str(exc))
            raise

    def convert_message_to_task(self, message_id: str, request: EmailToTaskRequest) -> TaskRecord:
        message = self.get_message(message_id)
        title = request.title or self._default_task_title(message)
        tags = self._normalize_tags([*request.tags, "email", "gmail"])
        description_parts = []
        if message.from_address:
            sender = message.from_display or message.from_address
            description_parts.append(f"Email from: {sender} <{message.from_address}>")
        if message.received_at:
            description_parts.append(f"Received: {message.received_at}")
        if message.subject:
            description_parts.append(f"Subject: {message.subject}")
        description_parts.append("")
        description_parts.append((message.body_text or message.snippet or "").strip())
        task_request = TaskCreateRequest.model_validate(
            {
                "title": title,
                "description": "\n".join(part for part in description_parts if part is not None).strip(),
                "priority": request.priority,
                "scheduled_date": request.scheduled_date,
                "due_at": request.due_at,
                "category": "email",
                "tags": tags,
            }
        )
        task = self._task_service.create_task(task_request)
        self._repository.add_email_task_link(message_id, task.id, iso_datetime(now_local()))
        return task

    def _prefs(self) -> dict[str, Any]:
        settings = self._settings_service.get()
        group = settings.get("google_email")
        return group if isinstance(group, dict) else {}

    def _token_snapshot(self) -> GmailTokenSnapshot | None:
        token = self._repository.get_google_oauth_token(GMAIL_PROVIDER)
        if token is None:
            return None
        return GmailTokenSnapshot(
            access_token=token.get("access_token"),
            refresh_token=token.get("refresh_token"),
            token_type=token.get("token_type"),
            scope=list(token.get("scope") or []),
            expires_at=token.get("expires_at"),
            email_address=token.get("email_address"),
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
            raise RuntimeError("Google email account is not connected.")
        if token.access_token and not self._token_is_expired(token):
            return token.access_token
        if not token.refresh_token:
            raise RuntimeError("Google email connection is missing a refresh token.")
        self._require_oauth_configured()
        refreshed = self._gateway.refresh_access_token(
            client_id=self._settings.google_oauth_client_id or "",
            client_secret=self._settings.google_oauth_client_secret or "",
            refresh_token=token.refresh_token,
        )
        self._persist_token(
            refreshed,
            email_address=token.email_address,
            refresh_token=token.refresh_token,
            last_error=None,
        )
        latest = self._token_snapshot()
        if latest is None or not latest.access_token:
            raise RuntimeError("Google email refresh did not produce a usable access token.")
        return latest.access_token

    def _persist_token(
        self,
        token_payload: dict[str, Any],
        *,
        email_address: str | None,
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
                "provider": GMAIL_PROVIDER,
                "access_token": str(token_payload.get("access_token") or "").strip() or None,
                "refresh_token": refresh_token,
                "token_type": str(token_payload.get("token_type") or "Bearer"),
                "scope": self._scope_list(token_payload.get("scope"), existing.scope if existing else []),
                "expires_at": expires_at,
                "email_address": email_address,
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
        self._repository.upsert_google_oauth_token(
            {
                "provider": GMAIL_PROVIDER,
                "access_token": token.access_token,
                "refresh_token": token.refresh_token,
                "token_type": token.token_type,
                "scope": token.scope,
                "expires_at": token.expires_at,
                "email_address": token.email_address,
                "last_sync_at": iso_datetime(now_local()),
                "last_error": None,
                "created_at": token.created_at,
                "updated_at": iso_datetime(now_local()),
            }
        )

    def _mark_sync_error(self, error_text: str) -> None:
        token = self._token_snapshot()
        if token is None:
            return
        self._repository.upsert_google_oauth_token(
            {
                "provider": GMAIL_PROVIDER,
                "access_token": token.access_token,
                "refresh_token": token.refresh_token,
                "token_type": token.token_type,
                "scope": token.scope,
                "expires_at": token.expires_at,
                "email_address": token.email_address,
                "last_sync_at": token.last_sync_at,
                "last_error": error_text,
                "created_at": token.created_at,
                "updated_at": iso_datetime(now_local()),
            }
        )

    def _require_draft(self, draft_id: str) -> dict[str, Any]:
        draft = self._repository.get_email_draft(draft_id)
        if draft is None:
            raise LookupError(f"Email draft '{draft_id}' not found")
        return draft

    def _decorate_messages(self, payloads: list[dict[str, Any]]) -> list[EmailMessageSummary]:
        linked = self._repository.list_email_task_links([str(item.get("id") or "") for item in payloads])
        items = []
        for payload in payloads:
            summary = self._message_summary_from_payload(payload)
            summary.linked_task_ids = linked.get(summary.id, [])
            items.append(summary)
        return items

    def _message_summary_from_payload(self, payload: dict[str, Any]) -> EmailMessageSummary:
        parsed = self._parse_message_payload(payload)
        return EmailMessageSummary.model_validate(parsed)

    def _message_detail_from_payload(self, payload: dict[str, Any]) -> EmailMessageDetail:
        parsed = self._parse_message_payload(payload)
        return EmailMessageDetail.model_validate(parsed)

    def _parse_message_payload(self, payload: dict[str, Any]) -> dict[str, Any]:
        message_payload = payload.get("payload") or {}
        headers = self._header_map(message_payload.get("headers") or [])
        text_parts, html_parts, has_attachments = self._extract_parts(message_payload)
        from_name, from_address = parseaddr(headers.get("from", ""))
        body_text = "\n\n".join(part for part in text_parts if part).strip()
        body_html = "\n".join(part for part in html_parts if part).strip() or None
        if not body_text and body_html:
            body_text = self._strip_html(body_html)
        subject = self._decode_mime_header(headers.get("subject", "")) or "(No subject)"
        received_at = self._received_at(payload, headers.get("date"))
        return {
            "id": str(payload.get("id") or ""),
            "thread_id": str(payload.get("threadId") or ""),
            "subject": subject,
            "from_display": from_name.strip(),
            "from_address": from_address.strip(),
            "to": self._recipient_list(headers.get("to")),
            "cc": self._recipient_list(headers.get("cc")),
            "snippet": str(payload.get("snippet") or "").strip(),
            "labels": [str(item) for item in payload.get("labelIds") or []],
            "is_read": "UNREAD" not in {str(item) for item in payload.get("labelIds") or []},
            "starred": "STARRED" in {str(item) for item in payload.get("labelIds") or []},
            "has_attachments": has_attachments,
            "received_at": received_at,
            "body_text": body_text,
            "body_html": body_html,
            "linked_task_ids": [],
        }

    def _default_task_title(self, message: EmailMessageDetail) -> str:
        base = (message.subject or message.snippet or "").strip()
        return f"Follow up: {base}" if base else "Follow up on email"

    def _normalize_tags(self, items: list[str]) -> list[str]:
        unique: list[str] = []
        seen: set[str] = set()
        for item in items:
            cleaned = item.strip()
            key = cleaned.casefold()
            if cleaned and key not in seen:
                unique.append(cleaned)
                seen.add(key)
        return unique

    def _normalize_label(self, value: str | None) -> str:
        cleaned = (value or "INBOX").strip().upper()
        return cleaned or "INBOX"

    def _normalize_limit(self, value: Any) -> int:
        try:
            numeric = int(value)
        except (TypeError, ValueError):
            numeric = 20
        return min(max(numeric, 1), 50)

    def _scope_list(self, value: Any, fallback: list[str]) -> list[str]:
        if isinstance(value, str):
            scopes = [item.strip() for item in value.split() if item.strip()]
            return scopes or list(fallback)
        if isinstance(value, list):
            scopes = [str(item).strip() for item in value if str(item).strip()]
            return scopes or list(fallback)
        return list(fallback)

    def _token_is_expired(self, token: GmailTokenSnapshot) -> bool:
        expires_at = parse_datetime(token.expires_at)
        if expires_at is None:
            return False
        return expires_at <= now_local() + timedelta(seconds=60)

    def _compose_raw_message(self, draft: dict[str, Any]) -> str:
        message = EmailMessage()
        message["To"] = ", ".join(draft.get("to") or [])
        if draft.get("cc"):
            message["Cc"] = ", ".join(draft.get("cc") or [])
        if draft.get("bcc"):
            message["Bcc"] = ", ".join(draft.get("bcc") or [])
        message["Subject"] = draft.get("subject") or ""
        message.set_content(draft.get("body_text") or "")
        return base64.urlsafe_b64encode(message.as_bytes()).decode("ascii").rstrip("=")

    def _header_map(self, headers: list[dict[str, Any]]) -> dict[str, str]:
        mapped: dict[str, str] = {}
        for item in headers:
            key = str(item.get("name") or "").strip().lower()
            if key:
                mapped[key] = str(item.get("value") or "")
        return mapped

    def _recipient_list(self, raw_value: str | None) -> list[str]:
        if not raw_value:
            return []
        return [address for _, address in getaddresses([raw_value]) if address]

    def _decode_mime_header(self, value: str) -> str:
        if not value:
            return ""
        try:
            return str(make_header(decode_header(value))).strip()
        except Exception:
            return value.strip()

    def _extract_parts(self, payload: dict[str, Any]) -> tuple[list[str], list[str], bool]:
        text_parts: list[str] = []
        html_parts: list[str] = []
        has_attachments = False

        def walk(part: dict[str, Any]) -> None:
            nonlocal has_attachments
            mime_type = str(part.get("mimeType") or "").strip().lower()
            filename = str(part.get("filename") or "").strip()
            body = part.get("body") or {}
            data = body.get("data")
            if filename:
                has_attachments = True
            decoded = self._decode_base64_body(data)
            if mime_type == "text/plain" and decoded:
                text_parts.append(decoded)
            elif mime_type == "text/html" and decoded:
                html_parts.append(decoded)
            for child in part.get("parts") or []:
                walk(child)

        walk(payload)
        return text_parts, html_parts, has_attachments

    def _decode_base64_body(self, value: Any) -> str:
        if not value or not isinstance(value, str):
            return ""
        padded = value + "=" * (-len(value) % 4)
        try:
            return base64.urlsafe_b64decode(padded.encode("ascii")).decode("utf-8", errors="ignore").strip()
        except Exception:
            return ""

    def _strip_html(self, value: str) -> str:
        compact = re.sub(r"<[^>]+>", " ", value)
        compact = html.unescape(compact)
        return re.sub(r"\s+", " ", compact).strip()

    def _received_at(self, payload: dict[str, Any], fallback_header: str | None) -> str | None:
        internal_date = payload.get("internalDate")
        try:
            if internal_date is not None:
                millis = int(str(internal_date))
                return datetime.fromtimestamp(millis / 1000, tz=timezone.utc).isoformat()
        except (TypeError, ValueError, OSError):
            pass
        return fallback_header.strip() if fallback_header else None
