from __future__ import annotations

from typing import Any


class FakeBrowserOpener:
    def __init__(self) -> None:
        self.opened_urls: list[str] = []

    def __call__(self, url: str) -> bool:
        self.opened_urls.append(url)
        return True


class FakePageFetcher:
    def __init__(self) -> None:
        self.requested_urls: list[str] = []

    def __call__(self, url: str) -> dict[str, Any]:
        self.requested_urls.append(url)
        return {
            "url": url,
            "status_code": 200,
            "title": "Example Domain",
            "summary": "Example Domain landing page",
        }


def _install_fake_runtime(client) -> tuple[FakeBrowserOpener, FakePageFetcher]:
    opener = FakeBrowserOpener()
    fetcher = FakePageFetcher()
    service = client.app.state.container.browser_automation_service
    service._browser_opener = opener
    service._page_fetcher = fetcher
    return opener, fetcher


def test_browser_automation_templates_and_run_lifecycle(client) -> None:
    opener, fetcher = _install_fake_runtime(client)

    templates_response = client.get("/v1/browser-automation/templates")
    assert templates_response.status_code == 200
    templates_payload = templates_response.json()
    assert templates_payload["count"] >= 2
    assert any(item["template_id"] == "open_page_review" for item in templates_payload["items"])

    create_response = client.post(
        "/v1/browser-automation/runs",
        json={
            "template_id": "open_page_review",
            "title": "Review product page",
            "goal": "Open the site, capture a page snapshot, and leave a human review checkpoint.",
            "inputs": {
                "start_url": "https://example.com/",
            },
        },
    )
    assert create_response.status_code == 200
    run = create_response.json()
    assert run["status"] == "awaiting_approval"
    assert run["current_step_index"] == 0
    assert [step["action_type"] for step in run["steps"]] == [
        "open_url",
        "fetch_page",
        "manual_checkpoint",
    ]
    assert run["steps"][0]["status"] == "pending_approval"
    assert run["logs"][0]["code"] == "run_created"

    history_response = client.get("/v1/browser-automation/runs")
    assert history_response.status_code == 200
    history_payload = history_response.json()
    assert history_payload["count"] == 1
    assert history_payload["items"][0]["id"] == run["id"]
    assert history_payload["items"][0]["pending_step_title"] == "Open the target page"

    approve_first = client.post(
        f"/v1/browser-automation/runs/{run['id']}/approve",
        json={"approval_note": "Open the page in the system browser."},
    )
    assert approve_first.status_code == 200
    after_first = approve_first.json()
    assert after_first["status"] == "awaiting_approval"
    assert after_first["current_step_index"] == 1
    assert after_first["steps"][0]["status"] == "completed"
    assert after_first["steps"][0]["result"]["opened"] is True
    assert opener.opened_urls == ["https://example.com/"]

    approve_second = client.post(
        f"/v1/browser-automation/runs/{run['id']}/approve",
        json={"approval_note": "Capture the page title for the audit log."},
    )
    assert approve_second.status_code == 200
    after_second = approve_second.json()
    assert after_second["status"] == "awaiting_approval"
    assert after_second["current_step_index"] == 2
    assert after_second["steps"][1]["status"] == "completed"
    assert after_second["steps"][1]["result"]["title"] == "Example Domain"
    assert fetcher.requested_urls == ["https://example.com/"]

    approve_third = client.post(
        f"/v1/browser-automation/runs/{run['id']}/approve",
        json={"approval_note": "Checkpoint accepted. Finish the run."},
    )
    assert approve_third.status_code == 200
    completed = approve_third.json()
    assert completed["status"] == "completed"
    assert completed["current_step_index"] == 3
    assert completed["completed_at"] is not None
    assert completed["steps"][2]["status"] == "completed"
    assert completed["steps"][2]["approval_note"] == "Checkpoint accepted. Finish the run."
    assert {entry["code"] for entry in completed["logs"]} >= {
        "run_created",
        "step_approved",
        "step_completed",
        "run_completed",
    }


def test_browser_automation_reject_and_cancel_paths(client) -> None:
    _install_fake_runtime(client)

    create_response = client.post(
        "/v1/browser-automation/runs",
        json={
            "template_id": "search_query_review",
            "title": "Review search results",
            "goal": "Launch a bounded search and stop before any follow-up action.",
            "inputs": {
                "query": "tauri desktop shell",
                "provider": "duckduckgo",
            },
        },
    )
    assert create_response.status_code == 200
    created = create_response.json()

    reject_response = client.post(
        f"/v1/browser-automation/runs/{created['id']}/reject",
        json={"reason": "Do not search this provider right now."},
    )
    assert reject_response.status_code == 200
    rejected = reject_response.json()
    assert rejected["status"] == "blocked"
    assert rejected["steps"][0]["status"] == "rejected"
    assert rejected["steps"][0]["approval_note"] == "Do not search this provider right now."
    assert rejected["logs"][-1]["code"] == "step_rejected"

    second_create = client.post(
        "/v1/browser-automation/runs",
        json={
            "template_id": "open_page_review",
            "title": "Cancel this run",
            "goal": "Create a run and cancel it before approving any step.",
            "inputs": {
                "start_url": "https://example.com/cancel",
            },
        },
    )
    assert second_create.status_code == 200
    second_run = second_create.json()

    cancel_response = client.post(
        f"/v1/browser-automation/runs/{second_run['id']}/cancel",
        json={"reason": "Operator cancelled the run."},
    )
    assert cancel_response.status_code == 200
    cancelled = cancel_response.json()
    assert cancelled["status"] == "cancelled"
    assert cancelled["cancelled_at"] is not None
    assert cancelled["steps"][0]["status"] == "cancelled"
    assert cancelled["logs"][-1]["code"] == "run_cancelled"


def test_browser_automation_rejects_unknown_template(client) -> None:
    response = client.post(
        "/v1/browser-automation/runs",
        json={
            "template_id": "unknown-template",
            "title": "Broken automation",
            "goal": "This should fail fast.",
            "inputs": {},
        },
    )
    assert response.status_code == 400
    assert "Unknown browser automation template" in response.json()["detail"]
