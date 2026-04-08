from __future__ import annotations

import re
import webbrowser
from dataclasses import dataclass
from typing import Any, Callable
from urllib.parse import quote_plus, urlparse

import httpx

from app.core.ids import make_id
from app.core.time import iso_datetime, now_local
from app.db.repository import SQLiteRepository
from app.models.schemas import (
    BrowserAutomationApprovalRequest,
    BrowserAutomationCancelRequest,
    BrowserAutomationLogRecord,
    BrowserAutomationRejectRequest,
    BrowserAutomationRunCreateRequest,
    BrowserAutomationRunDetail,
    BrowserAutomationRunListResponse,
    BrowserAutomationRunSummary,
    BrowserAutomationStepRecord,
    BrowserAutomationTemplateField,
    BrowserAutomationTemplateListResponse,
    BrowserAutomationTemplateRecord,
)


RunExecutor = Callable[[str], bool]
PageFetcher = Callable[[str], dict[str, Any]]


@dataclass(frozen=True)
class _TemplateStep:
    action_type: str
    title: str
    description: str
    url: str | None
    payload: dict[str, Any]
    recovery_notes: str


@dataclass(frozen=True)
class _TemplateSpec:
    template_id: str
    title: str
    description: str
    fields: list[BrowserAutomationTemplateField]
    build_steps: Callable[[dict[str, Any]], list[_TemplateStep]]


class BrowserAutomationService:
    def __init__(
        self,
        repository: SQLiteRepository,
        *,
        browser_opener: RunExecutor | None = None,
        page_fetcher: PageFetcher | None = None,
    ) -> None:
        self._repository = repository
        self._browser_opener = browser_opener or self._open_in_browser
        self._page_fetcher = page_fetcher or self._fetch_page_snapshot
        self._templates = self._build_templates()

    def list_templates(self) -> BrowserAutomationTemplateListResponse:
        items = [
            BrowserAutomationTemplateRecord(
                template_id=template.template_id,
                title=template.title,
                description=template.description,
                step_count=len(template.build_steps(self._template_preview_inputs(template.template_id))),
                fields=template.fields,
            )
            for template in self._templates.values()
        ]
        return BrowserAutomationTemplateListResponse(items=items, count=len(items))

    def list_runs(self, *, limit: int = 20) -> BrowserAutomationRunListResponse:
        items = [
            BrowserAutomationRunSummary.model_validate(item)
            for item in self._repository.list_browser_automation_runs(limit=limit)
        ]
        return BrowserAutomationRunListResponse(items=items, count=len(items))

    def create_run(self, request: BrowserAutomationRunCreateRequest) -> BrowserAutomationRunDetail:
        template = self._templates.get(request.template_id)
        if template is None:
            raise ValueError(f"Unknown browser automation template '{request.template_id}'.")

        normalized_inputs = self._normalize_inputs(request.inputs)
        built_steps = template.build_steps(normalized_inputs)
        if not built_steps:
            raise ValueError("Browser automation template did not produce any steps.")

        now_iso = iso_datetime(now_local())
        run_id = make_id("browser_run")
        start_url = next((step.url for step in built_steps if step.url), None)
        self._repository.create_browser_automation_run(
            {
                "id": run_id,
                "template_id": request.template_id,
                "title": request.title,
                "goal": request.goal,
                "status": "awaiting_approval",
                "current_step_index": 0,
                "start_url": start_url,
                "inputs": normalized_inputs,
                "created_at": now_iso,
                "updated_at": now_iso,
                "completed_at": None,
                "cancelled_at": None,
            }
        )
        self._repository.create_browser_automation_steps(
            [
                {
                    "id": make_id("browser_step"),
                    "run_id": run_id,
                    "position": index,
                    "action_type": step.action_type,
                    "title": step.title,
                    "description": step.description,
                    "status": "pending_approval" if index == 0 else "queued",
                    "requires_approval": True,
                    "url": step.url,
                    "payload": step.payload,
                    "result": {},
                    "approval_note": None,
                    "recovery_notes": step.recovery_notes,
                    "created_at": now_iso,
                    "updated_at": now_iso,
                    "completed_at": None,
                }
                for index, step in enumerate(built_steps)
            ]
        )
        self._log(
            run_id=run_id,
            step_id=None,
            level="info",
            code="run_created",
            message=f"Created browser automation run '{request.title}'.",
            payload={"template_id": request.template_id, "goal": request.goal},
        )
        return self.get_run(run_id)

    def get_run(self, run_id: str) -> BrowserAutomationRunDetail:
        run = self._require_run(run_id)
        steps = [
            BrowserAutomationStepRecord.model_validate(item)
            for item in self._repository.list_browser_automation_steps(run_id)
        ]
        logs = [
            BrowserAutomationLogRecord.model_validate(item)
            for item in self._repository.list_browser_automation_logs(run_id)
        ]
        summary = BrowserAutomationRunSummary.model_validate(
            {
                **run,
                "step_count": len(steps),
                "pending_step_title": self._pending_step_title(steps, run["current_step_index"]),
                "last_log_message": logs[-1].message if logs else None,
            }
        )
        return BrowserAutomationRunDetail(
            **summary.model_dump(),
            start_url=run.get("start_url"),
            inputs=dict(run.get("inputs") or {}),
            steps=steps,
            logs=logs,
        )

    def approve_next_step(
        self,
        run_id: str,
        request: BrowserAutomationApprovalRequest,
    ) -> BrowserAutomationRunDetail:
        run = self._require_active_run(run_id)
        step = self._require_pending_step(run_id)
        now_iso = iso_datetime(now_local())

        self._repository.update_browser_automation_run(
            run_id,
            {
                "status": "running",
                "updated_at": now_iso,
            },
        )
        self._repository.update_browser_automation_step(
            step["id"],
            {
                "status": "running",
                "approval_note": request.approval_note,
                "updated_at": now_iso,
            },
        )
        self._log(
            run_id=run_id,
            step_id=step["id"],
            level="info",
            code="step_approved",
            message=f"Approved step {step['position'] + 1}: {step['title']}.",
            payload={"approval_note": request.approval_note},
        )

        try:
            result = self._execute_step(step)
        except Exception as exc:
            error_text = str(exc)
            self._repository.update_browser_automation_step(
                step["id"],
                {
                    "status": "failed",
                    "approval_note": request.approval_note,
                    "result": {"error": error_text},
                    "updated_at": now_iso,
                },
            )
            self._repository.update_browser_automation_run(
                run_id,
                {
                    "status": "blocked",
                    "current_step_index": step["position"],
                    "updated_at": now_iso,
                },
            )
            self._log(
                run_id=run_id,
                step_id=step["id"],
                level="error",
                code="step_failed",
                message=f"Step '{step['title']}' failed.",
                payload={"error": error_text, "recovery_notes": step.get("recovery_notes")},
            )
            return self.get_run(run_id)

        self._repository.update_browser_automation_step(
            step["id"],
            {
                "status": "completed",
                "approval_note": request.approval_note,
                "result": result,
                "updated_at": now_iso,
                "completed_at": now_iso,
            },
        )
        self._log(
            run_id=run_id,
            step_id=step["id"],
            level="info",
            code="step_completed",
            message=f"Completed step {step['position'] + 1}: {step['title']}.",
            payload=result,
        )

        steps = self._repository.list_browser_automation_steps(run_id)
        next_step = self._find_next_step(steps, step["position"])
        if next_step is None:
            self._repository.update_browser_automation_run(
                run_id,
                {
                    "status": "completed",
                    "current_step_index": len(steps),
                    "updated_at": now_iso,
                    "completed_at": now_iso,
                },
            )
            self._log(
                run_id=run_id,
                step_id=None,
                level="info",
                code="run_completed",
                message=f"Completed browser automation run '{run['title']}'.",
                payload={"step_count": len(steps)},
            )
        else:
            self._repository.update_browser_automation_step(
                next_step["id"],
                {
                    "status": "pending_approval",
                    "updated_at": now_iso,
                },
            )
            self._repository.update_browser_automation_run(
                run_id,
                {
                    "status": "awaiting_approval",
                    "current_step_index": next_step["position"],
                    "updated_at": now_iso,
                },
            )

        return self.get_run(run_id)

    def reject_next_step(
        self,
        run_id: str,
        request: BrowserAutomationRejectRequest,
    ) -> BrowserAutomationRunDetail:
        self._require_active_run(run_id)
        step = self._require_pending_step(run_id)
        now_iso = iso_datetime(now_local())

        self._repository.update_browser_automation_step(
            step["id"],
            {
                "status": "rejected",
                "approval_note": request.reason,
                "updated_at": now_iso,
            },
        )
        self._repository.update_browser_automation_run(
            run_id,
            {
                "status": "blocked",
                "current_step_index": step["position"],
                "updated_at": now_iso,
            },
        )
        self._log(
            run_id=run_id,
            step_id=step["id"],
            level="warning",
            code="step_rejected",
            message=f"Rejected step {step['position'] + 1}: {step['title']}.",
            payload={"reason": request.reason, "recovery_notes": step.get("recovery_notes")},
        )
        return self.get_run(run_id)

    def cancel_run(
        self,
        run_id: str,
        request: BrowserAutomationCancelRequest,
    ) -> BrowserAutomationRunDetail:
        run = self._require_run(run_id)
        if run["status"] in {"completed", "cancelled"}:
            return self.get_run(run_id)

        now_iso = iso_datetime(now_local())
        for step in self._repository.list_browser_automation_steps(run_id):
            if step["status"] in {"queued", "pending_approval", "running"}:
                self._repository.update_browser_automation_step(
                    step["id"],
                    {
                        "status": "cancelled",
                        "approval_note": request.reason,
                        "updated_at": now_iso,
                    },
                )

        self._repository.update_browser_automation_run(
            run_id,
            {
                "status": "cancelled",
                "updated_at": now_iso,
                "cancelled_at": now_iso,
            },
        )
        self._log(
            run_id=run_id,
            step_id=None,
            level="warning",
            code="run_cancelled",
            message=f"Cancelled browser automation run '{run['title']}'.",
            payload={"reason": request.reason},
        )
        return self.get_run(run_id)

    def _execute_step(self, step: dict[str, Any]) -> dict[str, Any]:
        action_type = str(step.get("action_type") or "")
        url = step.get("url")
        if action_type == "open_url":
            if not isinstance(url, str) or not url:
                raise ValueError("Open URL step is missing a URL.")
            opened = bool(self._browser_opener(url))
            return {"url": url, "opened": opened}

        if action_type == "fetch_page":
            if not isinstance(url, str) or not url:
                raise ValueError("Fetch page step is missing a URL.")
            return dict(self._page_fetcher(url))

        if action_type == "manual_checkpoint":
            payload = dict(step.get("payload") or {})
            return {
                "checkpoint": payload.get("checkpoint") or "manual_review",
                "note": "Human checkpoint recorded. Review the visible browser state before any follow-up action.",
            }

        raise ValueError(f"Unsupported browser automation action '{action_type}'.")

    def _require_run(self, run_id: str) -> dict[str, Any]:
        run = self._repository.get_browser_automation_run(run_id)
        if run is None:
            raise LookupError(f"Browser automation run '{run_id}' not found.")
        return run

    def _require_active_run(self, run_id: str) -> dict[str, Any]:
        run = self._require_run(run_id)
        if run["status"] in {"completed", "cancelled"}:
            raise ValueError(f"Browser automation run '{run_id}' is already {run['status']}.")
        return run

    def _require_pending_step(self, run_id: str) -> dict[str, Any]:
        steps = self._repository.list_browser_automation_steps(run_id)
        for step in steps:
            if step["status"] == "pending_approval":
                return step
        raise ValueError(f"Browser automation run '{run_id}' has no pending approval step.")

    def _find_next_step(self, steps: list[dict[str, Any]], current_position: int) -> dict[str, Any] | None:
        for step in steps:
            if step["position"] == current_position + 1:
                return step
        return None

    def _pending_step_title(self, steps: list[BrowserAutomationStepRecord], current_step_index: int) -> str | None:
        for step in steps:
            if step.position == current_step_index and step.status in {"pending_approval", "queued", "running"}:
                return step.title
        return None

    def _log(
        self,
        *,
        run_id: str,
        step_id: str | None,
        level: str,
        code: str,
        message: str,
        payload: dict[str, Any],
    ) -> None:
        self._repository.create_browser_automation_log(
            {
                "id": make_id("browser_log"),
                "run_id": run_id,
                "step_id": step_id,
                "level": level,
                "code": code,
                "message": message,
                "payload": payload,
                "created_at": iso_datetime(now_local()),
            }
        )

    def _normalize_inputs(self, inputs: dict[str, Any]) -> dict[str, Any]:
        normalized: dict[str, Any] = {}
        for key, value in inputs.items():
            if isinstance(value, str):
                cleaned = value.strip()
                if cleaned:
                    normalized[key] = cleaned
            elif value is not None:
                normalized[key] = value
        return normalized

    def _template_preview_inputs(self, template_id: str) -> dict[str, Any]:
        if template_id == "search_query_review":
            return {"query": "desktop automation", "provider": "duckduckgo"}
        return {"start_url": "https://example.com/"}

    def _build_templates(self) -> dict[str, _TemplateSpec]:
        return {
            "open_page_review": _TemplateSpec(
                template_id="open_page_review",
                title="Open page review",
                description="Launch an approved page, capture a lightweight snapshot, and stop at a human checkpoint.",
                fields=[
                    BrowserAutomationTemplateField(
                        key="start_url",
                        label="Start URL",
                        placeholder="https://example.com/",
                        help_text="Only http and https targets are allowed.",
                    )
                ],
                build_steps=self._build_open_page_review_steps,
            ),
            "search_query_review": _TemplateSpec(
                template_id="search_query_review",
                title="Search query review",
                description="Open a bounded search query, snapshot the landing page, and wait for review before any next action.",
                fields=[
                    BrowserAutomationTemplateField(
                        key="query",
                        label="Search query",
                        placeholder="tauri desktop shell",
                        help_text="This becomes the provider query string.",
                    ),
                    BrowserAutomationTemplateField(
                        key="provider",
                        label="Provider",
                        required=False,
                        placeholder="duckduckgo",
                        help_text="Supported values: duckduckgo, bing, google.",
                    ),
                ],
                build_steps=self._build_search_query_review_steps,
            ),
        }

    def _build_open_page_review_steps(self, inputs: dict[str, Any]) -> list[_TemplateStep]:
        start_url = self._normalize_http_url(inputs.get("start_url"))
        return [
            _TemplateStep(
                action_type="open_url",
                title="Open the target page",
                description="Launch the approved page in the system browser.",
                url=start_url,
                payload={},
                recovery_notes="Check the URL and default browser configuration, then retry the run if the page did not open.",
            ),
            _TemplateStep(
                action_type="fetch_page",
                title="Capture a page snapshot",
                description="Fetch the current page title and response code for the audit log.",
                url=start_url,
                payload={"capture": "title"},
                recovery_notes="If the page could not be fetched, verify network access or confirm the page is reachable in a normal browser tab.",
            ),
            _TemplateStep(
                action_type="manual_checkpoint",
                title="Confirm the visible page state",
                description="Pause after the snapshot so a person can verify the page is correct before any follow-up action.",
                url=start_url,
                payload={"checkpoint": "review_visible_state"},
                recovery_notes="Cancel the run if the page content is not the intended destination or if the next action is no longer safe.",
            ),
        ]

    def _build_search_query_review_steps(self, inputs: dict[str, Any]) -> list[_TemplateStep]:
        query = str(inputs.get("query") or "").strip()
        if not query:
            raise ValueError("Search query review requires a non-empty query.")
        provider = str(inputs.get("provider") or "duckduckgo").strip().lower()
        search_url = self._search_url(provider, query)
        return [
            _TemplateStep(
                action_type="open_url",
                title="Open the search results",
                description="Launch the approved search query in the system browser.",
                url=search_url,
                payload={"provider": provider, "query": query},
                recovery_notes="Switch providers or narrow the query if the search target is not acceptable.",
            ),
            _TemplateStep(
                action_type="fetch_page",
                title="Snapshot the search landing page",
                description="Capture the page title and response metadata for the audit log.",
                url=search_url,
                payload={"provider": provider},
                recovery_notes="If the snapshot fails, verify provider availability and retry only after checking network access.",
            ),
            _TemplateStep(
                action_type="manual_checkpoint",
                title="Review results before continuing",
                description="Stop at a human checkpoint before any click-through or follow-up browser action.",
                url=search_url,
                payload={"checkpoint": "review_results"},
                recovery_notes="Cancel the run if the results are unexpected or if deeper interaction would require a fresh approval cycle.",
            ),
        ]

    def _search_url(self, provider: str, query: str) -> str:
        encoded = quote_plus(query)
        if provider == "duckduckgo":
            return f"https://duckduckgo.com/?q={encoded}"
        if provider == "bing":
            return f"https://www.bing.com/search?q={encoded}"
        if provider == "google":
            return f"https://www.google.com/search?q={encoded}"
        raise ValueError(f"Unsupported browser automation provider '{provider}'.")

    def _normalize_http_url(self, value: Any) -> str:
        url = str(value or "").strip()
        if not url:
            raise ValueError("Browser automation requires a start URL.")
        parsed = urlparse(url)
        if parsed.scheme not in {"http", "https"} or not parsed.netloc:
            raise ValueError("Browser automation only accepts valid http or https URLs.")
        return url

    def _open_in_browser(self, url: str) -> bool:
        return bool(webbrowser.open(url, new=2))

    def _fetch_page_snapshot(self, url: str) -> dict[str, Any]:
        try:
            response = httpx.get(url, timeout=10.0, follow_redirects=True)
        except httpx.HTTPError as exc:
            raise RuntimeError(f"Browser automation fetch failed: {exc}") from exc

        if response.is_error:
            raise RuntimeError(
                f"Browser automation fetch failed: {response.status_code} {response.reason_phrase}"
            )

        html = response.text
        title_match = re.search(r"<title[^>]*>(.*?)</title>", html, flags=re.IGNORECASE | re.DOTALL)
        title = re.sub(r"\s+", " ", title_match.group(1)).strip() if title_match else ""
        summary = re.sub(r"\s+", " ", re.sub(r"<[^>]+>", " ", html)).strip()[:180]
        return {
            "url": str(response.url),
            "status_code": response.status_code,
            "title": title or "Untitled page",
            "summary": summary or "No page summary available.",
        }
