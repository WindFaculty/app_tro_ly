from __future__ import annotations

import base64
from typing import Any


def _b64url(value: str) -> str:
    return base64.urlsafe_b64encode(value.encode("utf-8")).decode("ascii").rstrip("=")


def _message_payload(
    *,
    message_id: str,
    thread_id: str,
    subject: str,
    sender: str,
    to: str,
    cc: str = "",
    snippet: str,
    labels: list[str],
    body_text: str,
    body_html: str,
    internal_date: str,
) -> dict[str, Any]:
    headers = [
        {"name": "Subject", "value": subject},
        {"name": "From", "value": sender},
        {"name": "To", "value": to},
        {"name": "Date", "value": "Mon, 07 Apr 2026 09:00:00 +0000"},
    ]
    if cc:
        headers.append({"name": "Cc", "value": cc})
    return {
        "id": message_id,
        "threadId": thread_id,
        "labelIds": labels,
        "snippet": snippet,
        "internalDate": internal_date,
        "payload": {
            "mimeType": "multipart/alternative",
            "headers": headers,
            "parts": [
                {"mimeType": "text/plain", "body": {"data": _b64url(body_text)}},
                {"mimeType": "text/html", "body": {"data": _b64url(body_html)}},
            ],
        },
    }


class FakeGmailGateway:
    def __init__(self) -> None:
        self._messages = {
            "msg-1": _message_payload(
                message_id="msg-1",
                thread_id="thread-1",
                subject="Quarterly invoice review",
                sender="Billing Team <billing@example.com>",
                to="user@example.com",
                cc="ops@example.com",
                snippet="Please review the invoice before Friday.",
                labels=["INBOX", "UNREAD"],
                body_text="Please review the invoice before Friday.\nThanks.",
                body_html="<p>Please review the invoice before Friday.</p><p>Thanks.</p>",
                internal_date="1775552400000",
            ),
            "msg-2": _message_payload(
                message_id="msg-2",
                thread_id="thread-2",
                subject="Weekend plans",
                sender="Friend <friend@example.com>",
                to="user@example.com",
                snippet="Do you want to grab coffee this weekend?",
                labels=["INBOX"],
                body_text="Do you want to grab coffee this weekend?",
                body_html="<p>Do you want to grab coffee this weekend?</p>",
                internal_date="1775638800000",
            ),
        }
        self.sent_messages: list[str] = []

    def build_authorization_url(
        self,
        *,
        client_id: str,
        redirect_uri: str,
        state: str,
        scopes: list[str],
    ) -> str:
        return f"https://accounts.google.com/o/oauth2/v2/auth?state={state}&client_id={client_id}"

    def exchange_code(
        self,
        *,
        client_id: str,
        client_secret: str,
        code: str,
        redirect_uri: str,
    ) -> dict[str, Any]:
        return {
            "access_token": f"token-for-{code}",
            "refresh_token": "refresh-token",
            "token_type": "Bearer",
            "expires_in": 3600,
            "scope": "https://www.googleapis.com/auth/gmail.modify https://www.googleapis.com/auth/gmail.send",
        }

    def refresh_access_token(
        self,
        *,
        client_id: str,
        client_secret: str,
        refresh_token: str,
    ) -> dict[str, Any]:
        return {
            "access_token": "refreshed-token",
            "token_type": "Bearer",
            "expires_in": 3600,
            "scope": "https://www.googleapis.com/auth/gmail.modify https://www.googleapis.com/auth/gmail.send",
        }

    def get_profile(self, *, access_token: str) -> dict[str, Any]:
        return {"emailAddress": "user@example.com"}

    def list_messages(
        self,
        *,
        access_token: str,
        query: str,
        label: str | None,
        max_results: int,
    ) -> list[dict[str, Any]]:
        normalized_query = query.strip().lower()
        normalized_label = (label or "").strip().upper()
        items = []
        for payload in self._messages.values():
            if normalized_label and normalized_label not in {"ALL", "ANY"} and normalized_label not in payload["labelIds"]:
                continue
            haystack = " ".join(
                [
                    payload["snippet"],
                    payload["payload"]["headers"][0]["value"],
                    payload["payload"]["headers"][1]["value"],
                ]
            ).lower()
            if normalized_query and normalized_query not in haystack:
                continue
            items.append(payload)
        return items[:max_results]

    def get_message_detail(self, *, access_token: str, message_id: str) -> dict[str, Any]:
        return self._messages[message_id]

    def send_message(self, *, access_token: str, raw_message: str) -> dict[str, Any]:
        self.sent_messages.append(raw_message)
        return {"id": f"sent-{len(self.sent_messages)}"}


def _install_fake_gmail(client) -> FakeGmailGateway:
    gateway = FakeGmailGateway()
    container = client.app.state.container
    container.settings.google_oauth_client_id = "client-id"
    container.settings.google_oauth_client_secret = "client-secret"
    container.google_email_service._gateway = gateway
    container.google_email_service.disconnect_google_account()
    return gateway


def _connect_google_email(client) -> FakeGmailGateway:
    gateway = _install_fake_gmail(client)
    start = client.get("/v1/email/google/connect")
    assert start.status_code == 200
    state = start.json()["state"]
    callback = client.get("/v1/email/google/callback", params={"code": "demo-code", "state": state})
    assert callback.status_code == 200
    return gateway


def test_email_messages_stay_empty_when_google_is_not_configured(client) -> None:
    response = client.get("/v1/email/messages")
    assert response.status_code == 200
    payload = response.json()
    assert payload["count"] == 0
    assert payload["account"]["status"] == "not_configured"
    assert payload["account"]["configured"] is False


def test_google_email_auth_flow_and_settings_roundtrip(client) -> None:
    gateway = _install_fake_gmail(client)

    status_before = client.get("/v1/email/status")
    assert status_before.status_code == 200
    assert status_before.json()["status"] == "disconnected"
    assert status_before.json()["configured"] is True

    connect = client.get("/v1/email/google/connect")
    assert connect.status_code == 200
    assert "state=" in connect.json()["authorization_url"]

    callback = client.get(
        "/v1/email/google/callback",
        params={"code": "demo-code", "state": connect.json()["state"]},
    )
    assert callback.status_code == 200
    assert "Google email connected" in callback.text

    status_after = client.get("/v1/email/status")
    assert status_after.status_code == 200
    payload = status_after.json()
    assert payload["status"] == "ready"
    assert payload["email_address"] == "user@example.com"

    settings_update = client.put(
        "/v1/settings",
        json={"google_email": {"sync_enabled": False, "default_label": "all", "query_limit": 12}},
    )
    assert settings_update.status_code == 200
    settings_payload = settings_update.json()
    assert settings_payload["google_email"]["sync_enabled"] is False
    assert settings_payload["google_email"]["default_label"] == "all"
    assert settings_payload["google_email"]["query_limit"] == 12

    disabled_status = client.get("/v1/email/status")
    assert disabled_status.status_code == 200
    assert disabled_status.json()["status"] == "disabled"

    disconnect = client.post("/v1/email/google/disconnect")
    assert disconnect.status_code == 200
    assert disconnect.json()["status"] == "disconnected"
    assert gateway.sent_messages == []


def test_email_message_query_drafts_and_task_conversion(client) -> None:
    gateway = _connect_google_email(client)

    messages = client.get("/v1/email/messages", params={"query": "invoice", "label": "INBOX", "limit": 10})
    assert messages.status_code == 200
    payload = messages.json()
    assert payload["count"] == 1
    assert payload["items"][0]["subject"] == "Quarterly invoice review"
    assert payload["items"][0]["is_read"] is False

    detail = client.get("/v1/email/messages/msg-1")
    assert detail.status_code == 200
    detail_payload = detail.json()
    assert "Please review the invoice before Friday." in detail_payload["body_text"]
    assert detail_payload["linked_task_ids"] == []

    task_response = client.post(
        "/v1/email/messages/msg-1/task",
        json={"title": "Review invoice reply", "priority": "high", "tags": ["finance"]},
    )
    assert task_response.status_code == 200
    task_payload = task_response.json()
    assert task_payload["title"] == "Review invoice reply"
    assert task_payload["category"] == "email"
    assert set(task_payload["tags"]) == {"finance", "email", "gmail"}

    linked_detail = client.get("/v1/email/messages/msg-1")
    assert linked_detail.status_code == 200
    assert linked_detail.json()["linked_task_ids"] == [task_payload["id"]]

    draft = client.post(
        "/v1/email/drafts",
        json={
            "to": ["vendor@example.com"],
            "cc": ["ops@example.com"],
            "subject": "Invoice follow-up",
            "body_text": "Please send the corrected invoice.",
            "linked_message_id": "msg-1",
        },
    )
    assert draft.status_code == 200
    draft_payload = draft.json()
    assert draft_payload["status"] == "draft"

    sent = client.post(f"/v1/email/drafts/{draft_payload['id']}/send")
    assert sent.status_code == 200
    sent_payload = sent.json()
    assert sent_payload["status"] == "sent"
    assert sent_payload["gmail_message_id"] == "sent-1"
    assert len(gateway.sent_messages) == 1
